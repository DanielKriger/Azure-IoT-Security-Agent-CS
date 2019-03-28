﻿// <copyright file="IEventGenerator.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

using System.Collections.Generic;

namespace Microsoft.Azure.IoT.Contracts.Events
{
    /// <summary>
    /// Interface to be implemented by every event generator
    /// </summary>
    public interface IEventGenerator
    {
        /// <summary>
        /// Gets the priority of events generated by this event generator
        /// </summary>
        /// <returns>The priority of the events generated by this generator</returns>
        EventPriority Priority { get; }

        /// <summary>
        /// Tries to generate events and return them
        /// </summary>
        /// <returns>A list of generated events (could be empty)</returns>
        IEnumerable<IEvent> GetEvents();
    }
}