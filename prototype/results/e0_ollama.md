# E0 — Ollama probe

## 1. Version & models

```
{
 "version": "0.32.0",
 "models": [
  {
   "name": "gemma4:26b",
   "digest": "001e5dafc3c7"
  }
 ]
}
```

## 2. Teacher structured output (points) + speed

```
{
 "points": {
  "eat": 5,
  "flee": 85,
  "follow": 0,
  "wander": 0,
  "rest": 0,
  "mate": 0,
  "attack": 0
 },
 "gen_tok_s": 26.1,
 "prompt_tok_s": 210.7,
 "prompt_tokens": 286,
 "total_s": 46.4323489
}
```

## 3. seed+temperature 0 determinism

```
{
 "identical_3x": true
}
```

## 4. Logprobs (policy.mode=logprobs usable?)

```
{
 "response_keys": [
  "created_at",
  "done",
  "done_reason",
  "eval_count",
  "eval_duration",
  "load_duration",
  "logprobs",
  "message",
  "model",
  "prompt_eval_count",
  "prompt_eval_duration",
  "total_duration"
 ],
 "answer": "f",
 "raw_first": "{\"token\": \"f\", \"logprob\": 0, \"bytes\": [102], \"top_logprobs\": [{\"token\": \"f\", \"logprob\": 0, \"bytes\": [102]}, {\"token\": \"eat\", \"logprob\": -18.40239715576172, \"bytes\": [101, 97, 116]}, {\"token\": \"fear\", \"logprob\": -20.170917510986328, \"bytes\": [102, 101, 97, 114]}, {\"token\": \"attack\", \"logprob\": -20.73603630065918, \"bytes\": [97, 116, 116, 97, 99, 107]}, {\"token\": \"e\", \"logprob\": -21.166868209838867, ",
 "parsed_distribution": null,
 "logprobs_mode_usable": false
}
```

## 4b. Seconds per decision by mode (warm)

```
{
 "points": "median 2.88 s/decision",
 "logprobs": "median 0.52 s/decision"
}
```

## 5. Mutator samples

```
{
 "random change": "Snack when snacks are nearby.",
 "invert": "Avoid food whenever it is near.",
 "add a condition": "Eat whenever food is close and you are hungry."
}
```
