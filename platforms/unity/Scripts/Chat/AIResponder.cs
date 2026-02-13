using UnityEngine;

public class AIResponder : MonoBehaviour
{
    static readonly string[] Topics = {
        "movies","music","work","burnout","kids","gaming","habits","goals","friendship","travel"
    };

    public string GetReply(RoleType aiRole, string playerLast)
    {
        // MVP: deterministic-ish variety based on role
        string topic = Topics[Random.Range(0, Topics.Length)];
        bool asked = (playerLast != null && playerLast.Contains("?"));

        switch (aiRole)
        {
            case RoleType.Listener:
                return asked
                    ? Pick("That’s fair. What makes you ask?",
                           "I hear you. What part matters most to you?",
                           "I can answer, but what’s your take first?")
                    : Pick("That makes sense. Tell me more.",
                           "Good point—when did you first notice that?",
                           "What’s the best example you’ve seen?");

            case RoleType.Challenger:
                return asked
                    ? Pick("Before I answer, why does that matter to you?",
                           "What do you mean exactly—can you define it?",
                           "Are we assuming that’s true, or testing it?")
                    : Pick("What if the opposite is true?",
                           "What evidence would change your mind?",
                           "Is there a simpler explanation?");

            default: // Initiator / others
                return asked
                    ? Pick("Good question. I think " + topic + " matters because it shapes choices.",
                           "Honestly, " + topic + " affects my energy more than I expected.",
                           "For me, " + topic + " changes how I show up around people.")
                    : Pick("Let’s talk about " + topic + ". What’s your experience?",
                           "I’ve been thinking about " + topic + " lately—what do you think?",
                           "Topic idea: " + topic + ". Are you into that?");
        }
    }

    string Pick(string a, string b, string c)
    {
        int k = Random.Range(0, 3);
        return k == 0 ? a : (k == 1 ? b : c);
    }
}