# res://scripts/llm_connector.gd
class_name LLMConnector
extends Node

signal reply_received(text: String)

var _config: LLMConfig
var _http: HTTPRequest
var _fallback_role: int = -1
var _fallback_player_last: String = ""

func _ready() -> void:
	_config = LLMConfig.load_config()
	_http = HTTPRequest.new()
	_http.timeout = 15.0
	add_child(_http)
	_http.request_completed.connect(_on_request_completed)
	if _config != null:
		print("[LLMConnector] Active: %s / %s" % [_config.provider, _config.model])
	else:
		print("[LLMConnector] No config found, using local fallback")

func request_reply(ai_role: int, player_last: String, conversation_history: Array) -> void:
	_fallback_role = ai_role
	_fallback_player_last = player_last

	if _config == null:
		var local_reply := AIResponder.reply(ai_role, player_last)
		reply_received.emit(local_reply)
		return

	var system_prompt := _build_system_prompt(ai_role)
	var headers: PackedStringArray
	var body: Dictionary

	if _config.provider == "openai":
		headers = PackedStringArray([
			"Content-Type: application/json",
			"Authorization: Bearer %s" % _config.api_key,
		])
		body = {
			"model": _config.model,
			"max_tokens": _config.max_tokens,
			"temperature": _config.temperature,
			"messages": _build_openai_messages(system_prompt, player_last, conversation_history),
		}
	elif _config.provider == "anthropic":
		headers = PackedStringArray([
			"Content-Type: application/json",
			"x-api-key: %s" % _config.api_key,
			"anthropic-version: 2023-06-01",
		])
		body = {
			"model": _config.model,
			"max_tokens": _config.max_tokens,
			"temperature": _config.temperature,
			"system": system_prompt,
			"messages": _build_anthropic_messages(player_last, conversation_history),
		}
	else:
		push_warning("LLMConnector: unknown provider '%s'" % _config.provider)
		_use_fallback()
		return

	var json_body := JSON.stringify(body)
	var err := _http.request(_config.endpoint, headers, HTTPClient.METHOD_POST, json_body)
	if err != OK:
		push_warning("LLMConnector: HTTP request failed to start (err=%d)" % err)
		_use_fallback()

func _on_request_completed(result: int, response_code: int, _headers: PackedStringArray, body: PackedByteArray) -> void:
	if result != HTTPRequest.RESULT_SUCCESS or response_code < 200 or response_code >= 300:
		push_warning("LLMConnector: HTTP error result=%d code=%d" % [result, response_code])
		_use_fallback()
		return

	var json_text := body.get_string_from_utf8()
	var data: Variant = JSON.parse_string(json_text)
	if data == null:
		push_warning("LLMConnector: failed to parse response JSON")
		_use_fallback()
		return

	var text := _extract_reply_text(data)
	if text.is_empty():
		push_warning("LLMConnector: empty reply from API")
		_use_fallback()
		return

	reply_received.emit(text)

func _extract_reply_text(data: Dictionary) -> String:
	if _config.provider == "openai":
		var choices: Variant = data.get("choices", [])
		if choices is Array and choices.size() > 0:
			var msg: Variant = choices[0].get("message", {})
			if msg is Dictionary:
				return str(msg.get("content", ""))
	elif _config.provider == "anthropic":
		var content: Variant = data.get("content", [])
		if content is Array and content.size() > 0:
			return str(content[0].get("text", ""))
	return ""

func _use_fallback() -> void:
	var local_reply := AIResponder.reply(_fallback_role, _fallback_player_last)
	reply_received.emit(local_reply)

func _build_system_prompt(ai_role: int) -> String:
	var role_name: String = ScoringEngine.RoleType.keys()[ai_role]
	var role_desc: String
	match ai_role:
		ScoringEngine.RoleType.INITIATOR:
			role_desc = "You are the Initiator. You bring up new topics, propose ideas, and drive the conversation forward. Ask open-ended questions about interesting topics. Share your own perspectives to spark discussion. When the conversation stalls, introduce a fresh angle."
		ScoringEngine.RoleType.LISTENER:
			role_desc = "You are the Listener. You focus on understanding and validating what the other person says. Reflect their ideas back, ask clarifying follow-up questions, and show that you genuinely hear them. Use phrases like 'tell me more' or 'what matters most to you about that'. Avoid changing the subject."
		ScoringEngine.RoleType.CHALLENGER:
			role_desc = "You are the Challenger. You respectfully push back on ideas, ask for evidence, and test assumptions. Play devil's advocate when appropriate. Ask 'why' and 'what if the opposite were true'. Never be hostile -- be intellectually curious and constructively skeptical."
		ScoringEngine.RoleType.SYNTHESIZER:
			role_desc = "You are the Synthesizer. You connect ideas, find common threads, and summarize what has been discussed. Use phrases like 'it sounds like', 'in other words', and 'to bring these together'. Help the conversation reach deeper understanding by linking concepts."
		ScoringEngine.RoleType.EXPLORER:
			role_desc = "You are the Explorer. You are curious and adventurous in conversation. Ask unexpected questions, make creative connections between topics, and venture into new territory. Be enthusiastic about discovering new ideas together."
		_:
			role_desc = "You are a conversational partner."

	return "You are a conversational training partner in a communication skills game called ChatterGuild.\nYou are playing the role of %s.\n\n%s\n\nGuidelines:\n- Keep responses to 1-3 sentences. Be concise.\n- Stay in character for your role.\n- Do not mention that you are an AI.\n- Respond naturally as a human conversation partner would." % [role_name, role_desc]

func _build_openai_messages(system_prompt: String, player_last: String, history: Array) -> Array:
	var msgs: Array = [{"role": "system", "content": system_prompt}]
	for turn in history:
		msgs.append(turn)
	msgs.append({"role": "user", "content": player_last})
	return msgs

func _build_anthropic_messages(player_last: String, history: Array) -> Array:
	var msgs: Array = []
	for turn in history:
		msgs.append(turn)
	msgs.append({"role": "user", "content": player_last})
	return msgs
