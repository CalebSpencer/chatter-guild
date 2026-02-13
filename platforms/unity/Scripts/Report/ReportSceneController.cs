using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class ReportSceneController : MonoBehaviour
{
    public TMP_Text title;
    public TMP_Text body;

    PlayerProfile profile;

    void Start()
    {
        profile = SaveLoad.LoadOrCreate();
        var r = SessionCache.LastReport;

        if (r == null)
        {
            title.text = "No session report found.";
            body.text = "";
            return;
        }

        float i,l,c,s,e;
        r.GetArchetypeVector(out i, out l, out c, out s, out e);

        string className = Archetypes.GetClassName(profile.Initiator, profile.Listener, profile.Challenger, profile.Synthesizer, profile.Explorer);

        title.text = $"Session Report • +{r.TotalIP} IP";
        body.text =
            $"Level: {profile.Level}\n" +
            $"Class: {className}\n\n" +
            $"Archetype Evidence (this session):\n" +
            $"Initiator:   {i:0.00}\n" +
            $"Listener:    {l:0.00}\n" +
            $"Challenger:  {c:0.00}\n" +
            $"Synthesizer: {s:0.00}\n" +
            $"Explorer:    {e:0.00}\n\n" +
            $"Turns: {r.Turns.Count}\n" +
            $"(Add a scroll list later to show each turn’s OC/IN/IQ/RF + IP.)";
    }

    public void BackToMenu()
    {
        SceneManager.LoadScene("MenuScene");
    }
}