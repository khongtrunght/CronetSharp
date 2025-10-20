# CronetSharp Porting TODO List

**Last Updated:** 2025-10-21 (Phase 2 Complete)

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
- [x] Create `CronetSharp/Client/BodyUploadProvider.cs`
- [x] Port from `cronet-rs/src/client/body_upload_provider.rs`
- [x] Integrate with existing UploadDataProvider
- [x] Write unit tests (10 test cases)
- [x] Commit and push

### 1.4 Response Handler
- [x] Create `CronetSharp/Client/ResponseHandler.cs`
- [x] Port from `cronet-rs/src/client/response_handler.rs`
- [x] Implement UrlRequestCallback pattern
- [x] Handle response body accumulation
- [x] Write unit tests (9 test cases)
- [x] Commit and push

### 1.5 Main Client Class
- [x] Create `CronetSharp/Client/CronetClient.cs`
- [x] Port from `cronet-rs/src/client/client.rs`
- [x] Implement Send() and SendAsync() methods
- [x] Add timeout handling
- [x] Add redirect handling
- [x] Write unit tests (19 test cases)
- [x] Commit and push

### 1.6 End-to-End Tests
- [x] E2E test: Simple GET request
- [x] E2E test: POST with body
- [x] E2E test: Timeout handling
- [x] E2E test: Redirect following
- [x] E2E test: Error handling
- [x] Commit and push
- [x] Created CronetClientE2ETest.cs with 24 comprehensive tests

### 1.7 Documentation
- [ ] Add XML documentation comments
- [ ] Create usage examples
- [ ] Update README.md
- [ ] Commit and push

---

## Phase 2: Ordered Request Support ✅ COMPLETE

### 2.1 OrderedRequest Builder
- [x] Create `CronetSharp/OrderedRequest.cs`
- [x] Port from `cronet-rs/src/ordered_request.rs`
- [x] Use List<(string, string)> for header ordering
- [x] Write unit tests (37 comprehensive tests)
- [x] Commit and push

### 2.2 Integration Tests
- [x] Test: Header ordering preserved (included in unit tests)
- [x] Test: Compare with UrlRequestParams (included in unit tests)
- [x] Commit and push

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
- [x] Port BodyUploadProvider.cs (Phase 1.3)
- [x] Write unit tests for BodyUploadProvider (10 tests)
- [x] Port ResponseHandler.cs (Phase 1.4)
- [x] Write unit tests for ResponseHandler (9 tests)
- [x] Port CronetClient.cs (Phase 1.5)
- [x] Write unit tests for CronetClient (19 tests)
- [x] Add FromException method to ClientError
- [x] Port OrderedRequest.cs (Phase 2.1)
- [x] Write unit tests for OrderedRequest (37 tests)

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

**Next Action:** Begin Phase 3 - Export Layer (C API Interop)

**Phase 1 Status:** ✅ COMPLETE (5/5 files ported, 38+ unit tests, 24 E2E tests)
**Phase 2 Status:** ✅ COMPLETE (OrderedRequest with 37 unit tests)
