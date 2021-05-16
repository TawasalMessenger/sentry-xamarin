﻿using Android.Runtime;
using Sentry.Extensions;
using Sentry.Integrations;
using Sentry.Protocol;
using System;
using System.Collections.Generic;
using Xamarin.Essentials;

namespace Sentry.Xamarin.Internals
{
    internal class NativeIntegration : ISdkIntegration
    {
        private SentryXamarinOptions _xamarinOptions;
        private IHub _hub;

        internal NativeIntegration(SentryXamarinOptions options) => _xamarinOptions = options;

        /// <summary>
        /// Initialize the Android specific code.
        /// </summary>
        /// <param name="hub">The hub.</param>
        /// <param name="options">The Sentry options.</param>
        public void Register(IHub hub, SentryOptions options)
        {
            try
            {
                _hub = hub;
                Platform.ActivityStateChanged += Platform_ActivityStateChanged;
                AndroidEnvironment.UnhandledExceptionRaiser += AndroidEnvironment_UnhandledExceptionRaiser;
                JavaSdkInit(options);
            }
            catch (Exception ex)
            {
                options.DiagnosticLogger?.Log(SentryLevel.Error, "Sentry.Xamarin failed to attach AtivityStateChanged", ex);
                _xamarinOptions.NativeIntegrationEnabled = false;
            }
        }

        internal void Unregister()
        {
            if (_xamarinOptions.NativeIntegrationEnabled)
            {
                Platform.ActivityStateChanged -= Platform_ActivityStateChanged;
                IO.Sentry.Sentry.Close();
                IO.Sentry.Android.Ndk.SentryNdk.Close();
            }
        }

        private void JavaSdkInit(SentryOptions options)
        {
            IO.Sentry.Sentry.Init(options.ToSentryJavaOptions());
            IO.Sentry.Android.Core.SentryAndroid.Init(Platform.AppContext);
            IO.Sentry.Android.Ndk.SentryNdk.Init(options.ToSentryAndroidOptions());
            IO.Sentry.Sentry.CaptureMessage("Hello World from Native SDK");
        }

        private void Platform_ActivityStateChanged(object sender, ActivityStateChangedEventArgs e)
        {
            _hub.AddBreadcrumb(null,
                "ui.lifecycle",
                "navigation", data: new Dictionary<string, string>
                {
                    ["screen"] = _xamarinOptions.PageTracker?.CurrentPage,
                    ["state"] = e.State.ToString()
                }, level: BreadcrumbLevel.Info);
        }
        private void AndroidEnvironment_UnhandledExceptionRaiser(object sender, RaiseThrowableEventArgs e)
        {
            e.Exception.Data[Mechanism.HandledKey] = e.Handled;
            e.Exception.Data[Mechanism.MechanismKey] = "UnhandledExceptionRaiser";
            SentrySdk.CaptureException(e.Exception);
            if (!e.Handled)
            {
                SentrySdk.Close();
            }

        }
    }
}
