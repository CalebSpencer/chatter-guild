# res://scripts/llm_config.gd
class_name LLMConfig
extends RefCounted

var provider: String = ""
var model: String = ""
var api_key: String = ""
var endpoint: String = ""
var max_tokens: int = 300
var temperature: float = 0.7

static func load_config() -> LLMConfig:
	var paths := ["res://llm_config.json", "user://llm_config.json"]
	for path in paths:
		if not FileAccess.file_exists(path):
			continue
		var f := FileAccess.open(path, FileAccess.READ)
		if f == null:
			continue
		var json_text := f.get_as_text()
		f.close()

		var data: Variant = JSON.parse_string(json_text)
		if typeof(data) != TYPE_DICTIONARY:
			continue

		var cfg := LLMConfig.new()
		cfg.provider = str(data.get("provider", ""))
		cfg.model = str(data.get("model", ""))
		cfg.api_key = str(data.get("api_key", ""))
		cfg.endpoint = str(data.get("endpoint", ""))
		cfg.max_tokens = int(data.get("max_tokens", 300))
		cfg.temperature = float(data.get("temperature", 0.7))

		if cfg.api_key.is_empty() or cfg.provider.is_empty() or cfg.endpoint.is_empty():
			continue
		return cfg

	return null
