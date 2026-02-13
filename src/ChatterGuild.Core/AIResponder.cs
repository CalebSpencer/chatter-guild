using System;

namespace ChatterGuild.Core;

public static class AIResponder
{
    static readonly string[] Topics = { "movies","music","work","burnout","kids","gaming","habits","goals","friendship","travel" };

    public static string Reply(RoleType role, string playerLast, Random rng)
    {
        string topic = Topics[rng.Next(Topics.Length)];
        bool asked = (playerLast ?? "").Contains("?");

        string Pick(params string[] arr) => arr[rng.Next(arr.Length)];

        if (role == RoleType.Listener)
        {
            return asked
                ? Pick("That's fair. What makes you ask?",
                       "I hear you. What part matters most to you?",
                       "I can answer, but what's your take first?")
                : Pick("That makes sense. Tell me more.",
                       "Good point—when did you first notice that?",
                       "What's the best example you've seen?");
        }

        if (role == RoleType.Challenger)
        {
            return asked
                ? Pick("Before I answer, why does that matter to you?",
                       "What do you mean exactly—can you define it?",
                       "Are we assuming that's true, or testing it?")
                : Pick("What if the opposite is true?",
                       "What evidence would change your mind?",
                       "Is there a simpler explanation?");
        }

        return asked
            ? Pick($"Good question. I think {topic} matters because it shapes choices.",
                   $"Honestly, {topic} affects my energy more than I expected.",
                   $"For me, {topic} changes how I show up around people.")
            : Pick($"Let's talk about {topic}. What's your experience?",
                   $"I've been thinking about {topic} lately—what do you think?",
                   $"Topic idea: {topic}. Are you into that?");
    }
}
