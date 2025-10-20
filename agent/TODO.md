# CronetSharp Porting TODO List

**Last Updated:** 2025-10-21 (Updated by Claude)

---

## Phase 1: Client Module Foundation

### 1.1 Client Error Types
- [x] Create `CronetSharp/Client/ClientError.cs`
- [x] Port error enums from `cronet-rs/src/client/error.rs`
- [x] Write unit tests in `CronetSharp.Tests/Client/ClientErrorTest.cs`
- [x] Commit and push

### 1.2 Body Abstraction
- [x] Create `CronetSharp/Client/Body.cs`
- [x] Port from `cronet-rs/src/client/body.rs`
- [x] Support byte[], string, Stream, file paths
- [x] Write unit tests
- [x] Commit and push

### 1.3 Body Upload Provider
- [ ] Create `CronetSharp/Client/BodyUploadProvider.cs`
- [ ] Port from `cronet-rs/src/client/body_upload_provider.rs`
- [ ] Integrate with existing UploadDataProvider
- [ ] Write unit tests
- [ ] Commit and push

### 1.4 Response Handler
- [ ] Create `CronetSharp/Client/ResponseHandler.cs`
- [ ] Port from `cronet-rs/src/client/response_handler.rs`
- [ ] Implement UrlRequestCallback pattern
- [ ] Handle response body accumulation
- [ ] Write unit tests
- [ ] Commit and push

### 1.5 Main Client Class
- [ ] Create `CronetSharp/Client/CronetClient.cs`
- [ ] Port from `cronet-rs/src/client/client.rs`
- [ ] Implement Send() and SendAsync() methods
- [ ] Add timeout handling
- [ ] Add redirect handling
- [ ] Write unit tests
- [ ] Commit and push

### 1.6 End-to-End Tests
- [ ] E2E test: Simple GET request
- [ ] E2E test: POST with body
- [ ] E2E test: Timeout handling
- [ ] E2E test: Redirect following
- [ ] E2E test: Error handling
- [ ] Commit and push

### 1.7 Documentation
- [ ] Add XML documentation comments
- [ ] Create usage examples
- [ ] Update README.md
- [ ] Commit and push

---

## Phase 2: Ordered Request Support

### 2.1 OrderedRequest Builder
- [ ] Create `CronetSharp/OrderedRequest.cs`
- [ ] Port from `cronet-rs/src/ordered_request.rs`
- [ ] Use List<KeyValuePair> for header ordering
- [ ] Write unit tests
- [ ] Commit and push

### 2.2 Integration Tests
- [ ] Test: Header ordering preserved
- [ ] Test: Compare with UrlRequestParams
- [ ] Commit and push

---

## Phase 3: Export Layer

### 3.1 C API Exports
- [ ] Create `CronetSharp/Export/NativeApi.cs`
- [ ] Port from `cronet-rs/src/export/capi.rs`
- [ ] Implement CreateClient, FreeClient, SendRequest
- [ ] Use [UnmanagedCallersOnly] (.NET 5+)
- [ ] Write interop tests
- [ ] Commit and push

### 3.2 Memory Management
- [ ] Test: Memory leak detection
- [ ] Test: GC pressure test
- [ ] Commit and push

---

## Phase 4: Advanced Features

### 4.1 State Management
- [ ] Create `CronetSharp/State.cs`
- [ ] Port from `cronet-rs/src/state.rs`
- [ ] Write unit tests
- [ ] Commit and push

### 4.2 Status Listener
- [ ] Create `CronetSharp/UrlRequestStatusListener.cs`
- [ ] Port from `cronet-rs/src/url_request_status_listener.rs`
- [ ] Write unit tests
- [ ] Commit and push

---

## Ongoing Tasks

- [ ] Maintain commit hygiene (commit after every file)
- [ ] Run tests before each commit
- [ ] Keep TODO.md updated
- [ ] Update PORTING_PLAN.md with progress

---

## Completed Tasks

- [x] Explore cronet-rs repository
- [x] Explore CronetSharp repository
- [x] Create agent/ directory structure
- [x] Create PORTING_PLAN.md
- [x] Create TODO.md
- [x] Initial commit
- [x] Port ClientError.cs (Phase 1.1)
- [x] Port Body.cs (Phase 1.2)
- [x] Write unit tests for ClientError and Body

---

## Notes

- Remember 80/20 rule: 80% implementation, 20% testing
- Commit after EVERY file edit
- Use existing CronetSharp patterns for consistency
- Leverage GCManager for memory management

---

## Blockers

None currently.

---

## Questions to Resolve

1. Should we target .NET Standard 2.0 or upgrade to .NET 5+?
2. Do we need cross-platform support in Phase 1?
3. Should we use async/await throughout or match Rust's sync API?

---

**Next Action:** Port BodyUploadProvider.cs from cronet-rs/src/client/body_upload_provider.rs
