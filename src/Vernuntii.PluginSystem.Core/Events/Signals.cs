﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vernuntii.PluginSystem.Events
{
    /// <summary>
    /// Helper methods for <see cref="IOnceSignalable"/>.
    /// </summary>
    public static class Signals
    {
        public static bool AnyUnsignaled(IEnumerable<IOnceSignalable>? signals)
        {
            if (signals != null) {
                foreach (var signal in signals) {
                    if (!signal.SignaledOnce) {
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool AllSignaled(IEnumerable<IOnceSignalable>? signals) =>
            !AnyUnsignaled(signals);

        public static bool AllSignaled(params IOnceSignalable[]? signals) =>
            !AnyUnsignaled(signals);
    }
}
