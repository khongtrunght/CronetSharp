# CronetSharp Porting Plan

**Date Created:** 2025-10-21
**Status:** Active Development
**Goal:** Port cronet-rs (Rust) features to CronetSharp (C#)

---

## Executive Summary

The CronetSharp library is currently **100% complete** for low-level P/Invoke bindings but **lacks high-level convenience APIs** that exist in the Rust implementation. This plan focuses on porting the missing features while maintaining the 80/20 rule (80% implementation, 20% testing).

---

## Current State Analysis

### What's Already Complete in C# ✓
- All P/Invoke declarations for cronet.dll
- CronetEngine wrapper with builder pattern
- UrlRequest, UrlRequestCallback, UrlResponseInfo
- ByteBuffer, HttpHeader, Executor, Runnable
- Proxy configuration and parsing
- PublicKeyPins, QuicHint support
- Basic error handling (Error, ErrorCode)
- 14 NUnit test files
- 4 working examples (GET, POST, Proxy, MultiThreading)

### Missing Features from Rust Implementation ✗

#### Priority 1: Client Module (High-Level HTTP API)
**Files to port from cronet-rs:**
- `src/client/client.rs` - Main HTTP client wrapper
- `src/client/body.rs` - Request/response body abstraction
- `src/client/body_upload_provider.rs` - Body upload streaming
- `src/client/response_handler.rs` - Response processing
- `src/client/error.rs` - Client-specific error types

**Benefits:**
- Simplified API similar to HttpClient
- Automatic timeout handling
- Response body buffering
- Redirect management
- Connection pooling

#### Priority 2: Request Ordering & Utilities
**Files to port:**
- `src/ordered_request.rs` - Maintain header insertion order
- Request sequencing utilities

**Benefits:**
- Preserve HTTP header ordering
- Better control over concurrent requests

#### Priority 3: Export/Interop Layer
**Files to port:**
- `src/export/capi.rs` - C API exports (hsprotect_*)
- `src/export/capi_go.rs` - Go bindings (optional)

**Benefits:**
- Enable usage from C/C++
- COM interop scenarios

#### Priority 4: Advanced Features
**Files to port:**
- `src/state.rs` - State management
- `src/url_request_status_listener.rs` - Status monitoring
- Enhanced metrics collection

---

## Porting Strategy

### Phase 1: Client Module Foundation (Week 1-2)
**Goal:** Create high-level HTTP client API

1. **Port Client Error Types**
   - File: `CronetSharp/Client/ClientError.cs`
   - Source: `cronet-rs/src/client/error.rs`
   - Enums: CronetError, CancellationError, EngineError, TimeoutError

2. **Port Body Abstraction**
   - File: `CronetSharp/Client/Body.cs`
   - Source: `cronet-rs/src/client/body.rs`
   - Support: byte[], string, Stream, file paths

3. **Port Body Upload Provider**
   - File: `CronetSharp/Client/BodyUploadProvider.cs`
   - Source: `cronet-rs/src/client/body_upload_provider.rs`
   - Integrate with existing UploadDataProvider

4. **Port Response Handler**
   - File: `CronetSharp/Client/ResponseHandler.cs`
   - Source: `cronet-rs/src/client/response_handler.rs`
   - Implements UrlRequestCallback pattern
   - Accumulates response body chunks

5. **Port Main Client Class**
   - File: `CronetSharp/Client/CronetClient.cs`
   - Source: `cronet-rs/src/client/client.rs`
   - Methods: Send(), SendAsync(), Dispose()
   - Properties: DefaultTimeout, ShouldRedirect delegate

**Testing:**
- Unit tests for each component
- End-to-end test: Simple GET request
- End-to-end test: POST with body
- End-to-end test: Timeout handling
- End-to-end test: Redirect following

### Phase 2: Ordered Request Support (Week 3)
**Goal:** Preserve HTTP header ordering

1. **Port OrderedRequest Builder**
   - File: `CronetSharp/OrderedRequest.cs`
   - Source: `cronet-rs/src/ordered_request.rs`
   - Use List<KeyValuePair> instead of Dictionary

**Testing:**
- Unit test: Header ordering preserved
- Integration test: Compare with UrlRequestParams

### Phase 3: Export Layer (Week 4)
**Goal:** Enable C/C++ interop

1. **Port C API Exports**
   - File: `CronetSharp/Export/NativeApi.cs`
   - Source: `cronet-rs/src/export/capi.rs`
   - Functions: CreateClient, FreeClient, SendRequest
   - Use [UnmanagedCallersOnly] attribute (.NET 5+)

**Testing:**
- C++ interop test
- Memory leak test

### Phase 4: Advanced Features (Week 5)
**Goal:** Enhanced utilities

1. **Port State Management**
   - File: `CronetSharp/State.cs`
   - Source: `cronet-rs/src/state.rs`

2. **Port Status Listener**
   - File: `CronetSharp/UrlRequestStatusListener.cs`
   - Source: `cronet-rs/src/url_request_status_listener.rs`

**Testing:**
- State transition tests
- Status monitoring tests

---

## File Mapping Table

| Rust File | C# Target | Priority | Estimated Lines |
|-----------|-----------|----------|-----------------|
| client/client.rs | Client/CronetClient.cs | P1 | 300 |
| client/body.rs | Client/Body.cs | P1 | 150 |
| client/body_upload_provider.rs | Client/BodyUploadProvider.cs | P1 | 100 |
| client/response_handler.rs | Client/ResponseHandler.cs | P1 | 200 |
| client/error.rs | Client/ClientError.cs | P1 | 50 |
| ordered_request.rs | OrderedRequest.cs | P2 | 200 |
| export/capi.rs | Export/NativeApi.cs | P3 | 150 |
| state.rs | State.cs | P4 | 50 |
| url_request_status_listener.rs | UrlRequestStatusListener.cs | P4 | 100 |

**Total Estimated:** ~1,300 lines of C# code

---

## Testing Strategy (20% of effort)

### Unit Tests
- One test file per class
- Test public methods and properties
- Mock native calls where possible

### Integration Tests
- End-to-end HTTP scenarios
- Multi-threading tests
- Error handling tests

### Manual Testing
- Run examples against real servers
- Performance benchmarks vs HttpClient
- Memory leak detection

---

## Git Workflow

**Commit after EVERY file edit:**
```bash
git add <file>
git commit -m "feat: port <component> from Rust"
git push
```

**Commit message format:**
- `feat: <description>` - New feature ported
- `test: <description>` - New test added
- `fix: <description>` - Bug fix
- `docs: <description>` - Documentation update
- `refactor: <description>` - Code refactoring

---

## Success Criteria

### Milestone 1: Client Module Complete
- [ ] All 5 client files ported
- [ ] At least 5 end-to-end tests passing
- [ ] Example usage documented
- [ ] All changes committed and pushed

### Milestone 2: Ordered Request Complete
- [ ] OrderedRequest.cs implemented
- [ ] Header ordering tests passing
- [ ] Committed and pushed

### Milestone 3: Export Layer Complete
- [ ] NativeApi.cs implemented
- [ ] C++ interop test passing
- [ ] Committed and pushed

### Milestone 4: Advanced Features Complete
- [ ] State.cs and StatusListener.cs implemented
- [ ] Tests passing
- [ ] Committed and pushed

---

## Key Design Decisions

### C# Idioms to Use
1. **IDisposable** for resource management (not RAII)
2. **async/await** for Task-based async pattern
3. **Properties** instead of getter/setter methods
4. **Events** for callbacks (not delegates)
5. **Nullable reference types** (.NET 6+)
6. **Records** for immutable data structures

### Memory Management
- Use `GCHandle.Alloc/Free` for pinning
- Implement IDisposable pattern
- Leverage existing GCManager class

### Threading Model
- Use Task-based async APIs
- Preserve executor thread model
- Support CancellationToken

### Error Handling
- Use C# exceptions for recoverable errors
- Map Cronet error codes to custom exceptions
- Provide detailed error messages

---

## Dependencies to Add

### NuGet Packages
```xml
<PackageReference Include="System.Threading.Tasks.Extensions" Version="4.5.4" />
<PackageReference Include="System.Memory" Version="4.5.5" />
```

For .NET 5+ features:
```xml
<TargetFramework>net5.0</TargetFramework>
```

---

## Next Actions

1. Create `CronetSharp/Client/` directory
2. Port `ClientError.cs` (smallest file, easiest start)
3. Write unit tests for ClientError
4. Commit and push
5. Repeat for remaining files

---

## Progress Tracking

**Last Updated:** 2025-10-21

### Completed
- Initial analysis
- Long-term plan created

### In Progress
- None

### Blocked
- None

### Notes
- Focus on quality over speed
- Commit early and often
- Test as you go (not at the end)

---

## Resources

### Rust Documentation
- cronet-rs repository: `/Users/khongtrunght/work/captcha/convert_cronet/cronet-rs`
- Key files in `src/client/` directory

### C# Documentation
- CronetSharp repository: `/Users/khongtrunght/work/captcha/convert_cronet/CronetSharp`
- Existing patterns in `CronetSharp/` directory

### Cronet API Reference
- Header files in `cronet-rs/src/*.h`
- Official Chromium documentation

---

## Risk Mitigation

### Risk 1: P/Invoke Complexity
- **Mitigation:** Reuse existing patterns from CronetSharp codebase
- **Fallback:** Use simpler callback mechanisms

### Risk 2: Memory Leaks
- **Mitigation:** Extensive testing with memory profiler
- **Fallback:** Simplify lifetime management

### Risk 3: Threading Issues
- **Mitigation:** Use existing Executor/Runnable pattern
- **Fallback:** Add synchronization primitives

---

## Future Enhancements (Beyond Initial Port)

1. Cross-platform support (Linux, macOS)
2. Performance optimizations
3. Connection pooling
4. Request retry mechanisms
5. Enhanced logging/diagnostics
6. XML documentation comments
7. NuGet package publication

---

**End of Plan**
