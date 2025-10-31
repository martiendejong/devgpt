FolderToPostgres
================

Console app that ingests a folder of files into a Postgres-backed document store using JSON text storage and pgvector embeddings (via existing DevGPT.* projects). Uses OpenAI embeddings (out of the box) or a deterministic dummy fallback. HuggingFace support is available in the repo but not referenced by default.

Usage
-----

- Build: `dotnet build FolderToPostgres`
- Run: `dotnet run --project FolderToPostgres -- <folder> [--pattern <glob>] [--recurse] [--no-split]`

Examples:

- `dotnet run --project FolderToPostgres -- C:\data --pattern *.txt`
- `dotnet run --project FolderToPostgres -- ./docs --recurse`

Providers & Config
------------------

- `DEVGPT_PG_CONN` Postgres connection string
  - Default: `Host=localhost;Username=postgres;Password=postgres;Database=devgpt`
- Provider selection via `PROVIDER=openai|dummy` or auto by API keys
  - OpenAI: set `OPENAI_API_KEY` (or `DEVGPT_OPENAI_API_KEY`)
    - Optional: `OPENAI_EMBED_MODEL` (defaults `text-embedding-3-small` => dim 1536)
  - HuggingFace: supported by `DevGPT.HuggingFace` project but not referenced to avoid extra feeds; add a project reference and adjust code to enable if you need it
- `DEVGPT_EMBED_DIM` Embedding dimension override if needed
  - Defaults: OpenAI 1536 (3072 for 3-large), HF 768, Dummy 8

Notes
-----

- Requires the `vector` extension in Postgres; the app ensures `CREATE EXTENSION IF NOT EXISTS vector;`
- Stores files by relative path (forward slashes) as document keys.
- Uses a deterministic dummy embedding provider by default for easy local runs.
