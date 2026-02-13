# res://scripts/ai_responder.gd
class_name AIResponder
extends Node

static var topics := ["movies","music","work","burnout","kids","gaming","habits","goals","friendship","travel"]

static func reply(ai_role: int, player_last: String) -> String:
	var topic: String = topics[randi() % topics.size()]
	var asked := player_last.find("?") >= 0

	match ai_role:
		ScoringEngine.RoleType.LISTENER:
			if asked:
				return _pick(["That’s fair. What makes you ask?",
					"I hear you. What part matters most to you?",
					"I can answer, but what’s your take first?"])
			return _pick(["That makes sense. Tell me more.",
				"Good point—when did you first notice that?",
				"What’s the best example you’ve seen?"])

		ScoringEngine.RoleType.CHALLENGER:
			if asked:
				return _pick(["Before I answer, why does that matter to you?",
					"What do you mean exactly—can you define it?",
					"Are we assuming that’s true, or testing it?"])
			return _pick(["What if the opposite is true?",
				"What evidence would change your mind?",
				"Is there a simpler explanation?"])

		_:
			if asked:
				return _pick(["Good question. I think %s matters because it shapes choices." % topic,
					"Honestly, %s affects my energy more than I expected." % topic,
					"For me, %s changes how I show up around people." % topic])
			return _pick(["Let’s talk about %s. What’s your experience?" % topic,
				"I’ve been thinking about %s lately—what do you think?" % topic,
				"Topic idea: %s. Are you into that?" % topic])

static func _pick(arr: Array) -> String:
	return arr[randi() % arr.size()]