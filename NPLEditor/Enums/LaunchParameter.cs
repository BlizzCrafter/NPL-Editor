using System.Collections.Generic;
using System;

namespace NPLEditor.Enums
{
    /// <summary>
    /// Launch parameters to control the tool on startup. <br/>
    /// </summary>
    /// <remarks>
    /// <example> 
    /// Example 1: The tool will just build the content defined in the Content.npl json file and outputs no log information (silent mode).
    /// <code>
    /// npl-editor C:\Data\Content.npl build verbosity=silent 
    /// </code>
    /// </example>
    /// <example> 
    /// Example 2: The tool will launch with a window and GUI so you can edit the Content.npl file.
    /// <code>
    /// npl-editor C:\Data\Content.npl
    /// </code>
    /// </example>
    /// </remarks>
    public enum LaunchParameter
    {
        /// <summary>
        /// Parameterless build key.
        /// </summary>
        Build,

        /// <summary>
        /// <see cref="Serilog.Events.LogEventLevel"/>
        /// <code>Example: Verbosity=Debug</code> 
        /// </summary>
        Verbosity
    }

    /// <summary>
    /// Comparer for <see cref="LaunchParameter"/> enum.
    /// </summary>
    public class LaunchParameterComparer : IEqualityComparer<LaunchParameter>
    {
        /// <summary>
        /// Determines whether the specified <see cref="LaunchParameter"/> instances are equal.
        /// </summary>
        /// <param name="x">The first <see cref="LaunchParameter"/> to compare.</param>
        /// <param name="y">The second <see cref="LaunchParameter"/> to compare.</param>
        /// <returns>true if the specified <see cref="LaunchParameter"/> instances are equal; otherwise, false.</returns>
        public bool Equals(LaunchParameter x, LaunchParameter y)
        {
            return x.ToString().Equals(y.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Returns a hash code for the specified <see cref="LaunchParameter"/>.
        /// </summary>
        /// <param name="obj">The <see cref="LaunchParameter"/> for which a hash code is to be returned.</param>
        /// <returns>A hash code for the specified <see cref="LaunchParameter"/>.</returns>
        public int GetHashCode(LaunchParameter obj)
        {
            return obj.ToString().ToLowerInvariant().GetHashCode();
        }
    }
}
