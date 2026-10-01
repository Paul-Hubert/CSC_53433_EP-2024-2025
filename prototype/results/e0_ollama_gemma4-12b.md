# E0 — Ollama probe

## 1. Version & models

```
{
 "version": "0.32.0",
 "models": [
  {
   "name": "gemma4:12b",
   "digest": "6114515d63c1"
  },
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
  "eat": 25,
  "flee": 45,
  "follow": 5,
  "wander": 5,
  "rest": 10,
  "mate": 0,
  "attack": 0
 },
 "gen_tok_s": 89.7,
 "prompt_tok_s": 764.3,
 "prompt_tokens": 286,
 "total_s": 6.3482939
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
 "raw_first": "{\"token\": \"f\", \"logprob\": -9.536747711536009e-07, \"bytes\": [102], \"top_logprobs\": [{\"token\": \"f\", \"logprob\": -9.536747711536009e-07, \"bytes\": [102]}, {\"token\": \"eat\", \"logprob\": -14.027985572814941, \"bytes\": [101, 97, 116]}, {\"token\": \"e\", \"logprob\": -16.105621337890625, \"bytes\": [101]}, {\"token\": \"escape\", \"logprob\": -17.0522403717041, \"bytes\": [101, 115, 99, 97, 112, 101]}, {\"token\": \"F\", \"logpr",
 "parsed_distribution": null,
 "logprobs_mode_usable": false
}
```

## 4b. Seconds per decision by mode (warm)

```
{
 "points": "median 6.49 s/decision",
 "logprobs": "median 3.28 s/decision"
}
```

## 5. Mutator samples

```
{
 "random change": "Hunt when a prey animal appears nearby.",
 "invert": "Avoid eating whenever food is far away.",
 "add a condition": "Eat whenever food is close and you are hungry."
}
```
