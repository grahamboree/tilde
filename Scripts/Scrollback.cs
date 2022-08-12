using System.Text;

namespace Tilde {
    public class Scrollback {
        const string LOG_COLOR = "#586ED7";
        const string WARNING_COLOR = "#B58900";
        const string ERROR_COLOR = "#DC322F";

        const int MAX_SCROLLBACK_CHARS = 15000;

        string uiScrollBackCache;
        bool uiScrollBackCacheIsDirty = true;
        readonly StringBuilder uiScrollBack = new();

        string remoteScrollBackCache;
        bool remoteScrollBackCacheIsDirty = true;
        readonly StringBuilder remoteScrollBack = new();

        public void Append(string message, LogLineType messageType) {
            if (messageType == LogLineType.Normal) {
                uiScrollBack.Append("\n" + message);
            } else {
                string color = messageType == LogLineType.UnityLog ? LOG_COLOR
                    : messageType == LogLineType.Warning ? WARNING_COLOR
                    : ERROR_COLOR;
                uiScrollBack.Append($"\n<color={color}>{message}</color>");
            }
			
            remoteScrollBack.Append($"\n[{messageType}]{message}[/{messageType}]");

            uiScrollBackCacheIsDirty = true;
            remoteScrollBackCacheIsDirty = true;
            
            TrimScrollbacks();
        }

        public string ToUIString() {
            if (uiScrollBackCacheIsDirty) {
                uiScrollBackCache = uiScrollBack.ToString();
                uiScrollBackCacheIsDirty = false;
            }
            return uiScrollBackCache;
        }

        public string ToRemoteString() {
            if (remoteScrollBackCacheIsDirty) {
                remoteScrollBackCache = uiScrollBack.ToString();
                remoteScrollBackCacheIsDirty = false;
            }
            return remoteScrollBackCache;
        }

        void TrimScrollbacks() {
            int uiDelta = uiScrollBack.Length - MAX_SCROLLBACK_CHARS;
            if (uiDelta > 0) {
                uiScrollBack.Remove(0, uiDelta);
            }
            
            int remoteDelta = remoteScrollBack.Length - MAX_SCROLLBACK_CHARS;
            if (remoteDelta > 0) {
                remoteScrollBack.Remove(0, remoteDelta);
            }
        }
    }
}