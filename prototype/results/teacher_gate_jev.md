# Decision gate — JEV autotrust/JEV-9B@b63f651ce8ed:/root/.cache/huggingface/hub/models--autotrust--JEV-9B/snapshots/b63f651ce8ed64481d3f5e73ecdb05f740042f01/adapter_vllm (prompts/teacher_v5.md, template bare-v1)

| mode | sign acc | ΔP | MI_G founders | MI_G random | MI_O | failures | pass |
|---|---|---|---|---|---|---|---|
| jev | 1.00 | 0.376 | 0.143 | 0.014 | 0.943 | 0 | True |

| metric | value |
|---|---|
| MI_G founders (bits) | 0.143 |
| MI_G random text | 0.014 |
| MI_O founders | 0.943 |
| directed sign accuracy | 1.00 |
| directed mean ΔP | 0.376 |
| locality ratio (edit/unrelated) | nan |
| Spearman ρ text vs behaviour | nan |
| gibberish→neutral / founder→neutral | 0.010 / 0.056 |
| gates | {'G1': True, 'G2': False, 'G3': False} |

| locus | ΔP | sign acc | n |
|---|---|---|---|
| eat | 0.337 | 1.00 | 8 |
| flee | 0.177 | 1.00 | 10 |
| follow | 0.602 | 1.00 | 10 |
| rest | 0.341 | 1.00 | 12 |
| mate | 0.423 | 1.00 | 10 |
