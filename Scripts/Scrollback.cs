using System.Text;
using UnityEngine;

namespace Tilde {
    public class Scrollback {
        const string LOG_COLOR = "#586ED7";
        const string WARNING_COLOR = "#B58900";
        const string ERROR_COLOR = "#DC322F";

        readonly StringBuilder UIScrollBack = new();
        readonly StringBuilder RemoteScrollBack = new();

        public void Append(string message, LogLineType messageType) {
            if (messageType == LogLineType.Normal) {
                UIScrollBack.Append("\n" + message);
            } else {
                string color = messageType == LogLineType.UnityLog ? LOG_COLOR
                    : messageType == LogLineType.Warning ? WARNING_COLOR
                    : ERROR_COLOR;
                UIScrollBack.Append($"\n<color={color}>{message}</color>");
            }
			
            RemoteScrollBack.Append($"\n[{messageType}]{message}[/{messageType}]");
        }

        public string ToUIString() {
            return UIScrollBack.ToString();
        }

        public string ToRemoteString() {
            return RemoteScrollBack.ToString();
        }
    }
}