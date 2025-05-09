# CodeBuilder2 Component Usage Pattern Review

## Component/Service/Helper Usage in `CodeBuilder2`

### 1. Constructor Injection
- The primary approach to component/service/helper provisioning in `CodeBuilder2` is **constructor-based dependency injection**. The constructor receives as string/file path arguments:
  - `appDir`, `docStorePath`, `embedPath`, `partsFilePath`, `openAoApiKey`, `logFilePath`, `tempStorePath`, `tempStoreEmbeddingsFilePath`, `tempStorePartsFilePath`.
- Within the constructor, these are used to configure and instantiate the following core services/classes:
  - `OpenAIConfig` (created from API key)
  - `OpenAIClientWrapper` (created from OpenAIConfig)
  - `EmbeddingFileStore`, `TextFileStore`, `DocumentPartFileStore` (each created with storage/config arguments)
  - `DocumentStore` (created from stores above)
  - Likewise for temporary store: new instances of above with temp arguments
- The primary dependency objects are built, not injected as already-instantiated objects (except for string/path inputs and API key).

### 2. Service Instantiation Pattern
- All collaborating services (`OpenAIConfig`, `OpenAIClientWrapper`, `EmbeddingFileStore`, etc.) are **instantiated directly within the constructor** rather than looked up via a service locator or injected as interfaces.
  - There is **no use of IoC containers** or DI frameworks. All instantiation happens via `new ...` in the constructor.
- The list of available chat tools is constructed by direct instantiation of `DevGPTChatTool` objects in the constructor. These reference methods of `this` for their callbacks.

### 3. Static Resources
- Two static prompts (`CoderPrompt`, `ReviewerPrompt`) are loaded from disk at class-level initialization via `File.ReadAllText()`.

### 4. Properties/Fields
- Once instantiated, the dependencies (stores and tool contexts) are held in public or internal fields such as `Store`, `TempStore`, `ChatTools`, and others for later use by logic methods.

### 5. Usage/Pattern Summary
- **Canonical pattern:** Direct service instantiation in the constructor using parameters passed by the caller. Subsequent composition and wiring of helper objects and tool context collections is done imperatively.
- There is **no framework-level or attribute-driven dependency injection**. All dependencies must be supplied (via primitives/paths) and created in the constructor; the `CodeBuilder2` class owns the lifecycle of its helper/service objects.
- The dominant usage style is **manual, imperative construction and assembly**.

---

## TL;DR: Canonical Usage Approach

> The `CodeBuilder2` class uses a strictly manual constructor-based provisioning of services and helpers. All components (`DocumentStore`, OpenAI wrappers, file/embedding stores, chat tools, etc.) are directly instantiated inside the constructor using input parameters. There is neither IoC/DI nor service locator usage; all dependencies are assembled internally and managed by the class itself.