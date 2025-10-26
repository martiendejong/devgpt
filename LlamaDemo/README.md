LlamaDemo

Summary
- Minimal .NET 8 console app using LLamaSharp to run a local GGUF model and stream tokens to the console.
- CPU backend by default; optional CUDA/Metal/Vulkan backends for GPU.
- Includes notes and a small snippet showing how to kick off LoRA finetuning via llama.cpp (training is external).

Prerequisites
- .NET 8 SDK
- A quantized GGUF model file (e.g., `mistral-7b-instruct.Q4_K_M.gguf`).
- OS: Windows, Linux, or macOS.

Get a GGUF model
- Download from Hugging Face:
  - Example collections: `TheBloke/*` quantized models
  - Create a folder `models/` under the project and place your `.gguf` there.
- Optional GUI downloaders: LM Studio, Ollama (pull), etc. Just ensure you have a `.gguf` path.

Create and run
- From repo root:
  - `dotnet restore`
  - `dotnet run --project LlamaDemo`
- Or open the solution and run the `LlamaDemo` project.

Configuration
- Model path:
  - Default: `models/mistral-7b-instruct.Q4_K_M.gguf`
  - Override with env var `LLAMA_MODEL`.
- GPU acceleration:
  - Install a GPU backend package for LlamaDemo if desired:
    - `LLamaSharp.Backend.Cuda11` or `LLamaSharp.Backend.Cuda12` (Windows/Linux, NVIDIA)
    - `LLamaSharp.Backend.Metal` (macOS Apple Silicon)
    - `LLamaSharp.Backend.Vulkan` (cross‑platform Vulkan)
  - In `LlamaDemo/Program.cs:20`, set a positive `GpuLayerCount` to offload layers.

Code entrypoints
- `LlamaDemo/Program.cs:1` minimal streaming example using LLamaSharp.
- `LlamaDemo/TrainingDemo.cs:1` example on invoking llama.cpp LoRA finetune tool programmatically.

Training (LoRA / SFT)
- LLamaSharp focuses on inference. Training/fine‑tuning is performed with external tools:
  - llama.cpp examples/finetune (produces a LoRA adapter from a JSONL dataset)
  - Frameworks like Axolotl or Unsloth to train and export LoRA or full weights
- Typical LoRA flow with llama.cpp:
  1) Build llama.cpp and its examples to get the `finetune` tool.
  2) Prepare a JSONL dataset (e.g., Alpaca‑style SFT).
  3) Run: `finetune --model base.gguf --train-data data.jsonl --out-lora my_adapter.bin --epochs 1 --batch 4`
  4) Option A: Merge LoRA into base weights with llama.cpp merge tools to produce a new GGUF, then set `LLAMA_MODEL` to that GGUF and run LlamaDemo as usual.
  5) Option B: Use a runtime that supports loading LoRA adapters directly (if your chosen wrapper supports it). LLamaSharp currently emphasizes inference of GGUF weights; merging to GGUF is the most portable approach.

Programmatic finetune invocation
- See `LlamaDemo/TrainingDemo.cs:1` for a small helper that shells out to the `finetune` binary.
  - Example usage:
    - `await TrainingDemo.RunLoraFinetuneAsync(@"C:\\path\\to\\finetune.exe", @"models\\base.gguf", @"data\\train.jsonl", @"adapters\\my_adapter.bin", epochs: 1, batchSize: 4);`
  - Replace paths with your actual files.

Resource notes
- RAM: 16 GB recommended for 7B models on CPU.
- Larger models require more RAM/GPU memory and are slower.

