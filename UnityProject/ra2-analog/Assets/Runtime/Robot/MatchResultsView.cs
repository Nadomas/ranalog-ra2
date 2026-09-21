using UnityEngine;

namespace Ra2.Robot
{
    /// <summary>
    /// S10-02 minimal local-only results chrome (IMGUI). Displays host/authoritative summary only.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MatchResultsView : MonoBehaviour
    {
        [SerializeField] string sideLabel = "local";
        string body = "(no results)";
        bool visible;

        public string Body => body;
        public bool Visible => visible;

        public void Show(MatchSummary summary, string side = null)
        {
            if (!string.IsNullOrEmpty(side))
                sideLabel = side;
            body = MatchResultsStub.Format(summary, sideLabel);
            visible = true;
        }

        public void Hide()
        {
            visible = false;
        }

        void OnGUI()
        {
            if (!visible)
                return;

            const float w = 520f;
            const float h = 110f;
            var rect = new Rect(12f, 12f, w, h);
            GUI.Box(rect, "Match Results (thin)");
            GUI.Label(new Rect(rect.x + 10f, rect.y + 28f, w - 20f, h - 40f), body);
        }
    }
}
