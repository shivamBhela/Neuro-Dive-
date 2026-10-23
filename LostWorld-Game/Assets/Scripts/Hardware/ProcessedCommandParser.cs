using System.Text.RegularExpressions;
namespace LostWorlds.Person2.Hardware
{
    /// <summary>Strict command envelope only, not a biosignal parser.</summary>
    public static class ProcessedCommandParser
    {
        public const int MaximumBytes=256;
        static readonly Regex EffortEnvelope=new Regex("\\A[ \\t\\r]*\\{[ \\t\\r]*\"effort\"[ \\t\\r]*:[ \\t\\r]*([0-9]+(?:\\.[0-9]+)?)[ \\t\\r]*\\}[ \\t\\r]*\\z",RegexOptions.CultureInvariant);
        public static bool TryParseEffort(string message,out float effort)
        {
            effort=0;
            if(string.IsNullOrEmpty(message)||message.Length>MaximumBytes)return false;
            var match=EffortEnvelope.Match(message);
            return match.Success && float.TryParse(match.Groups[1].Value,System.Globalization.NumberStyles.AllowDecimalPoint,System.Globalization.CultureInfo.InvariantCulture,out effort) && !float.IsInfinity(effort) && effort>=0 && effort<=100;
        }
        static readonly Regex Envelope=new Regex("\\A[ \\t\\r]*\\{[ \\t\\r]*\"command\"[ \\t\\r]*:[ \\t\\r]*\"([A-Z]+)\"[ \\t\\r]*\\}[ \\t\\r]*\\z",RegexOptions.CultureInvariant);
        public static bool TryParse(string message,out HardwareCommand command)
        {
            command=default;
            if(string.IsNullOrEmpty(message)||message.Length>MaximumBytes)return false;
            var match=Envelope.Match(message);if(!match.Success)return false;
            switch(match.Groups[1].Value)
            {
                case "LEFT":command=HardwareCommand.LEFT;return true;
                case "RIGHT":command=HardwareCommand.RIGHT;return true;
                case "JUMP":command=HardwareCommand.JUMP;return true;
                case "GRAB":command=HardwareCommand.GRAB;return true;
                case "ACTIVATE":command=HardwareCommand.ACTIVATE;return true;
                default:return false;
            }
        }
    }
}
