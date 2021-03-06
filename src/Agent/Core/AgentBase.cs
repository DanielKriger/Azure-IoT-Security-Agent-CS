﻿// <copyright file="AgentBase.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

using Microsoft.Azure.IoT.Agent.Core.Configuration;
using Microsoft.Azure.IoT.Agent.Core.EventGeneration;
using Microsoft.Azure.IoT.Agent.Core.Exceptions;
using Microsoft.Azure.IoT.Agent.Core.Logging;
using Microsoft.Azure.IoT.Agent.Core.MessageWorker.Clients;
using Microsoft.Azure.IoT.Agent.Core.Providers;
using Microsoft.Azure.IoT.Agent.Core.Scheduling;
using System;
using System.Threading;
using System.Linq;
using Microsoft.Azure.IoT.Contracts.Events;

namespace Microsoft.Azure.IoT.Agent.Core
{
    /// <summary>
    /// A base class for agents
    /// </summary>
    public abstract class AgentBase : IDisposable
    {
        /// <summary>
        /// The application-wide cancellation token
        /// </summary>
        protected readonly CancellationTokenSource CancellationTokenSource = new CancellationTokenSource();

        /// <summary>
        /// Indicates if the agent has been disposed
        /// </summary>
        protected volatile bool IsDisposed;

        /// <summary>
        /// Ctor - creates a new agent harness object
        /// </summary>
        protected AgentBase()
        {
            SimpleLogger.Information("Agent is initializing...", sendAsDiagnostic: true);
            //Does not trigger on Windows due to bug https://github.com/dotnet/coreclr/issues/8565
            AppDomain.CurrentDomain.ProcessExit += Stop;
            AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionHandler;
        }

        /// <summary>
        /// Starts the agent execution
        /// Performs required initializations and calls abstract function DoOnStart() to perform any action required by the derived class 
        /// </summary>
        public void Start()
        {
            //Load the agent configurations
            try
            {
                AgentConfiguration.Init();
            }
            catch (Exception ex)
            {
                throw new AgentFailedToStartException("The agent can not be started", ex);
            }
       
            //Call derived class to perform their initializations/start
            DoOnStart();

        }

        /// <summary>
        /// Sets up the generation of events, as defined in the configuration, wit hthe proper intervals between them, to avoid overloading the system
        /// </summary>
        /// <param name="scheduler">The scheduler to setup the event generators in</param>
        protected void SetupEventGenerators(TaskScheduler scheduler)
        {
            var eventGeneratorsProvider = new AppConfigEventGeneratorsProvider();
            var generators = eventGeneratorsProvider.GetAll();
            DateTime timeToStartTask = DateTime.UtcNow;
            TimeSpan timeSpanBetweenGeneratorInvocation = new TimeSpan(LocalConfiguration.General.ProducerInterval.Ticks / generators.Count());

            foreach (var generator in generators)
            {
                EventProducer producer = new EventProducer(generator);
                scheduler.AddTask(producer, LocalConfiguration.General.ProducerInterval, timeToStartTask);
                timeToStartTask = timeToStartTask.Add(timeSpanBetweenGeneratorInvocation);
            }
        }

        /// <summary>
        /// This function will be called when the agent starts
        /// Implement here all the required actions before start
        /// </summary>
        protected abstract void DoOnStart();
        

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Report an error when an exception goes unhandled
        /// </summary>
        private void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs e)
        {
            SimpleLogger.Fatal("An exception was not handled", exception: (Exception) e.ExceptionObject);
        }

        /// <summary>
        /// Stop the agent execution
        /// </summary>
        private void Stop(object sender, EventArgs e)
        {
            SimpleLogger.Information("On stop Cancellation was requested, Disposing...", sendAsDiagnostic: true);
            Dispose();
        }

        /// <summary>
        /// Disposing the object
        /// </summary>
        /// <param name="disposing">Flag indicating dispose state</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing && !IsDisposed)
            {
                IsDisposed = true;
                AppDomain.CurrentDomain.ProcessExit -= Stop;
                AppDomain.CurrentDomain.UnhandledException -= UnhandledExceptionHandler;
                ExternalInterfaceFacade.DisposeInstance();
                CancellationTokenSource.Cancel();
                CancellationTokenSource.Dispose();
                SimpleLogger.CleanupLogger();
            }
        }
    }
}