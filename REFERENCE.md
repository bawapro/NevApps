# Framework reference

These are the .NET, C#, MAUI, Blazor, ASP.NET Core, and EF Core types that NevApps and NevAPI call. Each entry names the type, the properties and methods in use, and the idea an interview is usually testing.

[APIs removed from the NevAPI host](#apis-removed-from-the-nevapi-host) covers the JWT issuer, bearer handler, `DbContext` registration, and endpoint maps that the latest NevAPI commit stopped calling. Those entries are the framework types from that older host.

How the app is put together is in [ARCHITECTURE.md](ARCHITECTURE.md). Stored setting names are in [DATA-SPECS.md](DATA-SPECS.md).

## .NET

### `IServiceCollection`

`Microsoft.Extensions.DependencyInjection`. The list of services the host can construct. `MauiAppBuilder.Services` and `WebApplicationBuilder.Services` are both this collection. Registration describes how to build an object. The object is created later, when something asks for it.

| Method | What it registers |
| --- | --- |
| `AddSingleton<T>()` | One instance for the process. The container creates it on the first request and returns that same object afterward. |
| `AddSingleton<TService, TImplementation>()` | The same single instance, exposed as the service type. The implementation can differ per platform. |
| `AddSingleton<T>(Func<IServiceProvider, T>)` | A factory. The delegate receives the provider, so it can read configuration and then return the object. |
| `AddScoped<T>()` | One instance per scope. A scope is a unit of work the host opens and later disposes. |
| `AddTransient<T>()` | A new instance every time a consumer asks for the service. |
| `AddHttpClient()` | `IHttpClientFactory`, so callers receive an `HttpClient` whose handler lifetime the factory owns. |
| `AddDbContextFactory<TContext>()` | A factory that builds a new `DbContext` on demand. |
| `AddAuthentication` / `AddAuthorization` | The authentication schemes and the authorization policies. |
| `AddOpenApi()` | The OpenAPI document services. |

When the same service type is registered twice, the provider uses the last registration.

In an interview, name the lifetime and why the object is safe to share. A logger and a preferences store are shared for the process. A `DbContext` is one unit of work and is not safe to share across operations, so the code asks a factory for a new one and disposes it when the operation ends. A scoped service in a Blazor WebView lives for the scope that WebView opened.

### `IServiceProvider`

The object that creates services from the registrations.

| Member | Meaning |
| --- | --- |
| `GetRequiredService<T>()` | Returns the service. Throws `InvalidOperationException` when that type was never registered. |
| `GetService<T>()` | Returns the service, or null when it is missing. |
| `CreateScope()` | Opens a child scope. Disposing the scope disposes the scoped instances created from it. |
| `IServiceScope.ServiceProvider` | The provider that resolves services for that one scope. |

`MauiApp.Services` and `WebApplication.Services` are the root provider. It lives for the process. The Blazor WebView opens its own scope for the UI. A scoped service resolved from the root is tied to the root and lives until the process ends.

### `IConfiguration`

The tree of settings: appsettings files, environment variables, and, when startup adds it, Key Vault.

| Member | Meaning |
| --- | --- |
| `configuration["Key"]` | The value as a string, or null when the key is absent. |
| `AddAzureKeyVault(Uri, TokenCredential)` | Adds Key Vault secrets into the same tree. A provider added later supplies the value for a key it also defines. |

Startup reads connection settings and service endpoints from this tree. The values stay in configuration. Source code that needs a secret asks configuration at runtime.

### `IHostEnvironment`

| Member | Meaning |
| --- | --- |
| `IsDevelopment()` | True when the host environment name is Development. |
| `EnvironmentName` | The name, usually `Development`, `Staging`, or `Production`. `ASPNETCORE_ENVIRONMENT` sets it for a web app. |

Development-only work, such as mapping the OpenAPI document, sits inside this check.

### `HttpClient` and `IHttpClientFactory`

`HttpClient` sends HTTP requests. It is safe to share. The expensive part is the `HttpMessageHandler` underneath it, which owns sockets.

`AddHttpClient()` registers the factory. The container can then inject `HttpClient` into a constructor. The factory pools handlers and retires them on a timer, which keeps DNS changes visible and avoids socket exhaustion.

| Member | Meaning |
| --- | --- |
| `DefaultRequestHeaders` | Headers sent on every request from this client. |
| `TryAddWithoutValidation(name, value)` | Adds a header and skips the strict format check. Custom headers such as an API key use this. |
| `GetAsync(uri)` | Sends GET and returns `HttpResponseMessage`. |
| `GetFromJsonAsync<T>(uri)` | Sends GET and deserializes the body into `T`. |
| `PostAsJsonAsync(uri, payload, options)` | Serializes the payload as JSON and sends POST. |
| `SendAsync(request)` | Sends a request you built yourself, which is how a multipart upload is sent. |
| `Timeout` | How long one request may run before it is canceled. |

In an interview: the factory owns handler lifetime. A single shared client is the normal shape. Creating and disposing a new `HttpClient` on every call disposes the handler and can leave sockets in `TIME_WAIT`.

### `HttpRequestMessage`, `HttpContent`, `HttpResponseMessage`

| Type | Role |
| --- | --- |
| `HttpMethod` | The verb. `HttpMethod.Post` is the POST instance. |
| `HttpRequestMessage` | One request: method, URI, headers, and content. |
| `HttpContent` | The body. Dispose it with the request. |
| `StringContent` | A body from a string, with an encoding and a media type. |
| `StreamContent` | A body from a stream. |
| `MultipartFormDataContent` | A `multipart/form-data` body. `Add(content, name, fileName)` adds one part. The name must match the field the server reads. |
| `MediaTypeHeaderValue` | The `Content-Type` of one part, for example `image/jpeg`. |
| `HttpResponseMessage` | The status code, headers, and body. |
| `IsSuccessStatusCode` | True for status codes 200 through 299. |
| `EnsureSuccessStatusCode()` | Returns the response for a success code and throws `HttpRequestException` otherwise. |
| `StatusCode` | The `HttpStatusCode` value. |

`using` on the response disposes the content stream.

### `JsonSerializer`

`System.Text.Json`. `Serialize(value)` writes an object to a JSON string. `JsonSerializerOptions.PropertyNamingPolicy` chooses the JSON names. `null` keeps the CLR property names. `JsonNamingPolicy.CamelCase` writes `propertyName`. The phone posts JSON with a null policy so the names match the API models.

### `Task` and `Task<T>`

`System.Threading.Tasks`. A task is a handle to work that finishes later. `Task` has no result. `Task<T>` has a result of type `T`.

| Member | Meaning |
| --- | --- |
| `Status` | `WaitingToRun`, `Running`, `RanToCompletion`, `Canceled`, or `Faulted`, among others. |
| `IsCompleted` | True when the task reached a final state, including faulted and canceled. |
| `IsFaulted` | True when the task threw. |
| `Exception` | An `AggregateException` when the task faulted. |
| `Result` | The value of a `Task<T>`. Reading it before the task completes blocks the calling thread. |
| `Task.Run(action)` | Queues the delegate on the thread pool and returns a task for it. |
| `Task.CompletedTask` | An already successful task. Useful as a default. |
| `ContinueWith(action, scheduler)` | Schedules a follow-up. The scheduler chooses the thread that runs it. |
| `GetAwaiter().GetResult()` | Blocks until the task finishes and throws the original exception. |
| `WaitAsync(cancellationToken)` | Returns a task that completes when the original task completes or the token is canceled. |

`async` and `await` are how you consume a task. See the C# section.

Startup uses `Task.Run` so SQLite model compilation and migration overlap the rest of host setup. Callers that need the database wait on that same task.

In an interview: `await` releases the thread until the task finishes. `.Result`, `.Wait()`, and `GetResult()` hold the thread. Holding a thread that the task needs in order to resume is a deadlock. Work started with `Task.Run` does not capture a UI synchronization context, so a later blocking wait can still finish.

### `TaskScheduler` and `AppDomain`

| Member | Meaning |
| --- | --- |
| `TaskScheduler.Default` | The thread-pool scheduler. |
| `TaskScheduler.UnobservedTaskException` | Raised when a faulted task is garbage-collected and nobody observed the exception. |
| `UnobservedTaskExceptionEventArgs.Exception` | The `AggregateException`. |
| `UnobservedTaskExceptionEventArgs.SetObserved()` | Marks the exception as seen so the runtime treats it as handled. |
| `AppDomain.CurrentDomain.UnhandledException` | Raised for an exception that escaped every `catch` on a thread. |
| `UnhandledExceptionEventArgs.ExceptionObject` | The thrown object, typed as `object`. Cast it to `Exception`. |
| `UnhandledExceptionEventArgs.IsTerminating` | True when the process is exiting because of this exception. |

A task that nobody awaits, and whose exception nobody reads, becomes unobserved. The event is the last place to log it. `SetObserved` records that the process has seen it.

### `Exception`

| Member | Meaning |
| --- | --- |
| `Message` | The text the thrower supplied. |
| `InnerException` | The earlier exception this one wraps. |
| `StackTrace` | The frames at the throw site. |
| `AggregateException` | Holds one or more exceptions. Tasks use it because several children can fail. |
| `GetBaseException()` | Walks `InnerException` to the innermost exception. |
| `Flatten()` | Unwraps nested aggregate exceptions into one list. |
| `InvalidOperationException` | The operation is illegal in the current state, including a missing required configuration value. |

Catch the most specific type you can handle. Log the base exception when an aggregate is only a wrapper.

### `Lazy<T>`

Delays construction until `.Value` is read. The default thread-safety mode runs the factory once, even if two threads read `.Value` together. The OCR plugin is held this way so it is created on the first scan.

### `Stopwatch`, `Environment`, `Debug`

| Member | Meaning |
| --- | --- |
| `Stopwatch.StartNew()` | Starts a timer and returns it. |
| `ElapsedMilliseconds` | Milliseconds since the timer started. |
| `Environment.CurrentManagedThreadId` | The id of the managed thread now running. |
| `Debug.WriteLine` | Writes a line to the debug output. A release build can strip `Debug` calls. |

### `IDisposable` and `IAsyncDisposable`

`Dispose()` releases unmanaged resources: file handles, database connections, HTTP content, JS object references. `await using` calls `DisposeAsync()`.

A `using` declaration disposes at the end of the block:

```csharp
using var db = await factory.CreateDbContextAsync();
```

Dispose a `DbContext` when the unit of work is finished. Dispose a `DotNetObjectReference` when the component goes away. Dispose an `HttpResponseMessage` when you have finished reading the body.

### `CancellationToken`

A signal that the caller no longer wants the operation. Pass it into `CreateDbContextAsync`, HTTP calls, and blob uploads. Check `IsCancellationRequested`, or call `ThrowIfCancellationRequested()`. A token from an `HttpContext.RequestAborted` cancels when the client disconnects.

### `ILoggerFactory` and `ILogger<T>`

`ILoggerFactory.CreateLogger(category)` builds a logger. `ILogger<T>.LogInformation`, `LogWarning`, and `LogError` write at a level. The category is usually the class name, which is what `ILogger<T>` uses. ASP.NET constructs an `AuthenticationHandler` with an `ILoggerFactory`.

`builder.Logging.AddDebug()` sends log output to the debug listener in debug builds.

## C#

### `async` and `await`

`async` on a method lets the body use `await`. `await` records where to resume, returns the current thread to the caller, and continues after the task completes.

| Signature | What the caller can do |
| --- | --- |
| `async Task` | Await the method. Exceptions are stored on the task. |
| `async Task<T>` | Await the method and receive `T`. |
| `async void` | Matches an event handler, which returns void. Exceptions go to the synchronization context. |

`await` on a `Task` that already completed continues without a yield. After an await in a UI or Blazor context, execution resumes on that context, so you can touch component state. `ConfigureAwait(false)` resumes on a thread-pool thread and is used in library code that has no UI to return to.

In an interview, walk one method: it hits `await`, the thread is free to paint the UI, the task completes, the method continues on the captured context.

### Generics

`Task<T>`, `List<T>`, `IDbContextFactory<TContext>`, and `EventCallback<T>` are open types closed over a type argument. A constraint limits the argument:

```csharp
private async Task<T> GetAsync<T>(string endpoint) where T : new()
```

`where T : new()` means `T` has a public parameterless constructor, so `new T()` compiles. `where T : class` means a reference type. `where TContext : DbContext` means the factory only builds context types.

### Nullable reference types

`string?` may be null. `string` is expected to be non-null when the project has nullable enabled. `Expense?` on a parameter says the parent may pass null. The `!` operator tells the compiler you have already checked, as in `configuration["APIUrl"]!`. A failed check still throws at runtime.

### `DateOnly` and `DateTime`

| Member | Meaning |
| --- | --- |
| `DateOnly` | A calendar date with no time and no offset. |
| `DateOnly.FromDateTime(dateTime)` | Keeps the date part. |
| `DateOnly.Parse(text)` | Reads a date. `"yyyy-MM-dd"` is the round-trip form used in SQLite. |
| `ToString("yyyy-MM-dd")` | Writes that form. |
| `DateTime.Now` | Local time. |
| `DateTime.UtcNow` | UTC. |
| `TryParseExact(text, formats, culture, styles, out value)` | Parses only the listed patterns. Returns false when none match. |
| `DateTimeStyles.None` | No extra whitespace or timezone assumptions. |
| `DateTimeOffset` | A timestamp plus its offset from UTC. Document Intelligence date fields use it. |

`DateOnly` is the right type for a receipt date or a reminder date. `DateTime` carries a time of day, which those rows do not have. SQLite has no date type, so EF stores the `DateOnly` as text through a value converter.

### `CultureInfo` and `RegionInfo`

| Member | Meaning |
| --- | --- |
| `CultureInfo.InvariantCulture` | A culture that does not change with the phone language. Parsing machine values uses it. |
| `new CultureInfo("en-CA")` | A specific culture. Date patterns and number formats follow it. |
| `DateTimeFormat.AbbreviatedMonthNames` | `Jan`, `Feb`, and the rest, in that culture. |
| `RegionInfo` | A country or region. Built from a culture name such as `en-CA`. |
| `RegionInfo.EnglishName` | The English country name. |
| `RegionInfo.ISOCurrencySymbol` | The currency code, such as `CAD`. |
| `RegionInfo.TwoLetterISORegionName` | The region code, such as `CA`. |

Compare culture-sensitive strings with an explicit `StringComparison`. `OrdinalIgnoreCase` compares code points and ignores case, which is the comparison for identifiers and keys.

### `string`

| Member | Meaning |
| --- | --- |
| `IsNullOrEmpty` | True for null or `""`. |
| `IsNullOrWhiteSpace` | True for null, `""`, or only whitespace. |
| `Trim` | Removes leading and trailing whitespace. |
| `ToLowerInvariant` | Lowercase using invariant culture, so a key does not change with the phone language. |
| `string.Equals(a, b, comparison)` | Compares with the comparison you name. |

### `Path`, `File`, and streams

| Member | Meaning |
| --- | --- |
| `Path.Combine(a, b)` | Joins path segments with the platform separator. |
| `Path.GetTempPath()` | The user temporary directory. Design-time migrations use a file there. |
| `Path.GetFileName(path)` | The last segment. |
| `File.Exists(path)` | True when that file is present. |
| `File.Copy(source, dest, overwrite)` | Copies bytes. `overwrite: true` replaces a file that is already there. |
| `FileStream` | A stream over a file. |
| `FileMode.Open` | Open an existing file. |
| `FileAccess.Read` | Read only. |
| `FileShare.ReadWrite` | Other handles may read and write while this one is open. A log file that is still being written needs this. |
| `Stream` | A sequence of bytes. `ReadAsync`, `CopyToAsync`, `Length`, `Position`. |
| `MemoryStream` | A stream over a byte array in memory. |
| `StreamReader` | Reads text from a stream with an encoding. |
| `Encoding.UTF8` | UTF-8. `StringContent` and file names in this solution use it. |

Dispose every stream. A file copy of the SQLite database waits until pooled connections have released the file. See `SqliteConnection.ClearAllPools` in the DB section.

### `Aes` and `ICryptoTransform`

`System.Security.Cryptography`. `Aes.Create()` returns an AES algorithm object. `Key` and `IV` are the secret and the initialization vector. `CreateEncryptor(key, iv)` and `CreateDecryptor(key, iv)` return an `ICryptoTransform`. `TransformFinalBlock` processes one buffer. A `CryptoStream` wraps another stream and transforms bytes as they pass through.

The key and the IV are secrets. An interview answer puts them in configuration or a key vault, and treats a value compiled into the binary as visible to anyone who has the assembly. This document does not copy the key.

### Conditional compilation

`#if ANDROID`, `#elif IOS`, and `#if DEBUG` keep or drop source before it is compiled. The target framework defines `ANDROID` or `IOS`. The Debug configuration defines `DEBUG`. The binary for one platform contains only the branch that was true for that build. A platform service is registered inside the matching branch, so the other platform's type is absent from that assembly.

### `partial`

`partial class` and `partial method` split one type across files. The compiler merges the parts. A Razor component is a partial class: the `.razor` file is one part, and the generated code is the other.

### `record`

`record` is a reference type with value-based equality, a compiler-generated constructor for the positional parameters, and `with` expressions that copy the record and change named properties. A `record struct` is the value-type form. The API project template uses a positional record for the sample forecast type.

## MAUI

### `MauiApp` and `MauiAppBuilder`

The composition root of a .NET MAUI app.

| Member | Meaning |
| --- | --- |
| `MauiApp.CreateBuilder()` | Starts a builder with the MAUI defaults. |
| `UseMauiApp<TApp>()` | Sets the `Application` subclass that owns windows. |
| `ConfigureFonts` | Registers font files and the alias components use. |
| `Services` | The `IServiceCollection`. |
| `Logging` | The logging builder. |
| `Build()` | Builds the `IServiceProvider` and returns the `MauiApp`. |
| `MauiApp.Services` | The root `IServiceProvider`. |
| `AddMauiBlazorWebView()` | Registers the services a Blazor WebView needs. |
| `AddBlazorWebViewDeveloperTools()` | Enables the Blazor debug tools. Registered in the debug build. |

`CreateMauiApp` is the method every platform entry point calls. Android, iOS, Mac Catalyst, and Windows each have a small platform class whose override returns `MauiProgram.CreateMauiApp()`.

### Platform entry types

| Type | Role |
| --- | --- |
| `MauiApplication` | The Android `Application`. Its `CreateMauiApp` override builds the MAUI app. |
| `MauiAppCompatActivity` | The Android activity that hosts MAUI. |
| `MauiUIApplicationDelegate` | The iOS `AppDelegate` base. |
| `UIApplication.Main` | The iOS entry. It starts UIKit and names the `AppDelegate`. |
| `BlazorWebView` | The native control that hosts the Blazor renderer inside a `ContentPage`. |
| `ContentPage` | One native page. The main page hosts the WebView. |
| `Window` | A native window. `Application.CreateWindow` returns the window the platform shows. |

### `FileSystem`

| Member | Meaning |
| --- | --- |
| `FileSystem.AppDataDirectory` | The app's private data directory. The SQLite file lives here. The OS backs it up with the app on the platforms that back up app data. |
| `FileSystem.CacheDirectory` | A directory the OS may empty. Scratch files and intermediate images go here. |

`Path.Combine(FileSystem.AppDataDirectory, fileName)` is the full path.

### `IPreferences`

Small key/value pairs that survive process death: flags, names, and last-known values. This is separate from the SQLite settings table.

| Member | Meaning |
| --- | --- |
| `Preferences.Default` | The app's preference store. |
| `Get(key, defaultValue)` | The stored value, or `defaultValue` when the key is absent. The type of the default picks the overload. |
| `Set(key, value)` | Writes a bool, string, int, long, float, double, or `DateTime`. |
| `Remove(key)` | Deletes one key. |
| `Clear()` | Deletes every preference for the app. |
| `ContainsKey(key)` | True when the key is present. |

Preferences suit a handful of scalars. Rows of expenses belong in the database.

### `Permissions`

MAUI asks the OS for a capability and reports a `PermissionStatus`: `Unknown`, `Granted`, `Denied`, `Disabled`, `Restricted`, or `Limited`.

| Member | Meaning |
| --- | --- |
| `CheckStatusAsync<TPermission>()` | The current status. It does not show a prompt. |
| `RequestAsync<TPermission>()` | Shows the platform prompt when the OS still allows one. |
| `Permissions.Camera` | Camera permission. |
| `Permissions.LocationWhenInUse` | Location while the app is in the foreground. |
| `Permissions.LocationAlways` | Location in the background. The OS treats this more strictly. |
| `Permissions.BasePlatformPermission` | The base for a permission MAUI does not ship. Activity recognition on Android is implemented by subclassing it and listing the platform permission strings. |

Check first. Request when the status is still undecided. A second request after a permanent denial does not show the dialog again. The user then changes the permission in system settings.

### `MediaPicker` and `FileResult`

| Member | Meaning |
| --- | --- |
| `MediaPicker.Default` | The shared picker. |
| `CapturePhotoAsync(options)` | Opens the camera and returns a `FileResult`, or null if the user cancels. |
| `PickPhotoAsync(options)` | Opens the gallery. |
| `MediaPickerOptions` | Optional title and rotation behavior. |
| `FileResult.FileName` | The file name. |
| `FileResult.ContentType` | The media type. |
| `FileResult.FullPath` | The path, when the platform exposes one. |
| `FileResult.OpenReadAsync()` | A readable stream of the bytes. |

The stream is what you upload or pass to OCR. Dispose it when the upload finishes.

### `Geocoding`

| Member | Meaning |
| --- | --- |
| `Geocoding.Default.GetPlacemarksAsync(latitude, longitude)` | Reverse geocodes a coordinate. |
| `GetPlacemarksAsync(location)` | The same, from a `Location`. |
| `Placemark` | Address parts: `FeatureName`, `Thoroughfare`, `Locality`, `AdminArea`, `CountryName`, `PostalCode`. |

The call needs location permission and a network on most platforms. It can return an empty sequence.

### `MainThread`

UI kit objects are touched on the main thread.

| Member | Meaning |
| --- | --- |
| `MainThread.IsMainThread` | True when the caller is already on the main thread. |
| `BeginInvokeOnMainThread(action)` | Queues the action on the main thread and returns. |
| `InvokeOnMainThreadAsync(action)` | Queues the action and returns a task that completes when it finishes. |

A callback from a location or sensor API arrives on a background thread. Updates that flow into MAUI or Blazor state are posted back with `BeginInvokeOnMainThread`.

## Blazor

A `.razor` file compiles to a component class that derives from `ComponentBase`. The markup is the render tree. The `@code` block is the class body.

### `Router`, `RouteView`, `RouteData`

| Member | Meaning |
| --- | --- |
| `Router` | Finds the component whose route matches the current URI. |
| `AppAssembly` | The assembly whose components are scanned for routes. |
| `Found` | The template used when a route matches. Its `Context` is a `RouteData`. |
| `NotFound` | The template used when nothing matches. |
| `RouteView` | Renders the matched component inside a layout. |
| `RouteData` | The page type and the route parameters. |
| `DefaultLayout` | The layout type wrapped around every page that does not set its own. |
| `FocusOnNavigate` | Moves focus to the element matching a CSS selector after navigation. |

### `@page`

`@page "/expense-tracker/add"` registers that component at that relative URI. A route can contain a parameter, `@page "/items/{id:int}"`. The constraint (`int`, `guid`, `datetime`) rejects values that do not convert. The matching component property is marked `[Parameter]`.

`@inject NavigationManager NavigationManager` asks the scope for that service and assigns the property. `@implements IDisposable` adds the interface so the renderer calls `Dispose` when the component is removed.

### `NavigationManager`

| Member | Meaning |
| --- | --- |
| `NavigateTo(uri)` | Goes to a route. The default does a client-side navigation. |
| `NavigateTo(uri, forceLoad: true)` | Loads the URI with a full browser load. |
| `Uri` | The absolute URI. |
| `BaseUri` | The base href. |
| `ToBaseRelativePath(uri)` | The path relative to `BaseUri`, which is what you compare with route templates. |
| `LocationChanged` | An event raised after navigation. The args carry the new location. |

Subscribe in `OnInitialized` and unsubscribe in `Dispose`. A handler that outlives the component keeps the component alive and runs against a disposed renderer.

### `ComponentBase` lifecycle

The renderer calls these in order for a normal first render.

| Method | When it runs |
| --- | --- |
| `SetParametersAsync` | The renderer is applying parameters. The base implementation sets the properties and then calls the methods below. |
| `OnInitialized` | Once, after the first parameters are set. Synchronous work. |
| `OnInitializedAsync` | Once, after `OnInitialized`. This is where the first database or HTTP load belongs. |
| `OnParametersSet` | Every time parameters are applied, including the first time. |
| `OnParametersSetAsync` | The async pair of `OnParametersSet`. |
| `OnAfterRender(firstRender)` | After the render has been sent to the browser. |
| `OnAfterRenderAsync(firstRender)` | The async pair. `firstRender` is true only on the first render. JS interop that needs an element runs here, because the element exists. |
| `ShouldRender()` | Return false to skip a render. |
| `StateHasChanged()` | Marks the component dirty so the renderer renders it again. |
| `InvokeAsync(work)` | Runs the delegate on the renderer's synchronization context. A callback from a timer, a task continuation, or JavaScript uses this before it touches state. |

`OnInitializedAsync` that awaits does not block the UI thread. After the await, call `StateHasChanged` when you changed fields and the renderer does not already know. Parameter changes and event handlers already schedule a render.

In an interview, give the order: parameters, init once, parameters again on later sets, render, then `OnAfterRenderAsync`. Explain that JS that touches an element waits for `firstRender`.

### `[Parameter]`, `EventCallback`, `RenderFragment`

| Member | Meaning |
| --- | --- |
| `[Parameter]` | A value the parent passes in markup. The child treats it as input. |
| `[Parameter] public EventCallback<T> NameChanged` | The conventional pair for two-way binding on `Name`. |
| `EventCallback` | A callback the parent can await. It has no payload. |
| `EventCallback<T>` | A callback with a payload of type `T`. |
| `HasDelegate` | True when the parent supplied a handler. |
| `InvokeAsync(value)` | Calls the handler and returns its task. |
| `RenderFragment` | A chunk of markup. `ChildContent` is the fragment between the component's tags. |
| `[CascadingParameter]` | A value supplied by an ancestor `CascadingValue`, or by the framework. Dialog content receives the dialog instance this way. |

`EventCallback` knows the receiver. Invoking it renders the receiver after the handler. A raw `Action` does not.

### `ElementReference`, `IJSRuntime`, `DotNetObjectReference`

| Member | Meaning |
| --- | --- |
| `@ref="field"` | Stores an `ElementReference` for that element. |
| `ElementReference` | A handle you can pass to JavaScript. It is valid after the element has rendered. |
| `IJSRuntime.InvokeVoidAsync(identifier, args)` | Calls a JS function and returns when the promise resolves. No .NET result. |
| `IJSRuntime.InvokeAsync<T>(identifier, args)` | Calls a JS function and deserializes the result as `T`. |
| `DotNetObjectReference.Create(component)` | A handle JavaScript can use to call back into this instance. |
| `DotNetObjectReference.Dispose()` | Drops the handle so the component can be collected. |
| `[JSInvokable]` | Marks a public method JavaScript may call. |

The pattern in this app: after the first render, create a `DotNetObjectReference`, pass it to JS with the element, and let JS call a `[JSInvokable]` method when a gesture finishes. `Dispose` detaches JS and disposes the reference.

In an interview: JS cannot see a .NET object unless you pass a `DotNetObjectReference`. Forgetting `Dispose` pins the component for the life of the page.

## API

### `WebApplication` and `WebApplicationBuilder`

The host for an ASP.NET Core minimal API.

| Member | Meaning |
| --- | --- |
| `WebApplication.CreateBuilder(args)` | Builds configuration from args, environment variables, and `appsettings`, and creates the service collection. |
| `builder.Configuration` | The `IConfiguration`. |
| `builder.Environment` | The `IHostEnvironment`. |
| `builder.Services` | The `IServiceCollection`. |
| `builder.Build()` | Creates the `WebApplication`. |
| `UseAuthentication()` | Runs the authentication middleware, which builds the `ClaimsPrincipal` for the request. |
| `UseAuthorization()` | Runs authorization. Place it after `UseAuthentication`. |
| `UseHttpsRedirection()` | Redirects HTTP requests to HTTPS. |
| `Run()` | Starts listening and blocks until shutdown. |
| `MapOpenApi()` | Exposes the OpenAPI document. Mapped when the environment is Development. |

Middleware order is the order of the `Use` calls. Authentication must see the request before authorization decides.

### Minimal API endpoints

Extension methods on `IEndpointRouteBuilder`, which `WebApplication` implements.

| Member | Meaning |
| --- | --- |
| `MapGet(pattern, handler)` | Routes GET to the delegate. |
| `MapPost(pattern, handler)` | Routes POST. |
| `MapPut` / `MapDelete` | PUT and DELETE. |
| `RouteHandlerBuilder` | The object `MapGet` returns. Further calls add metadata. |
| `AllowAnonymous()` | This endpoint skips the fallback authorization policy. |
| `Accepts<T>(contentType)` | Declares the request body type for OpenAPI, for example `IFormFile` as `multipart/form-data`. |
| `WithName(name)` | Sets the endpoint name used by link generation. |

A route parameter in braces, `api/items/{id}`, binds to a handler parameter of the same name. A constraint such as `{id:int}` rejects a value that does not convert, and the framework returns 400.

The handler can be `async` and can return `IResult`, a model object, or a plain value. A model object is written as JSON with status 200.

### `IResult` and `Results`

`IResult.ExecuteAsync(HttpContext)` writes the response. `Results` is a factory of the common ones.

| Method | Status |
| --- | --- |
| `Results.Ok(value)` | 200 with a JSON body. |
| `Results.Created(uri, value)` | 201 with a `Location` header. |
| `Results.NoContent()` | 204 and an empty body. A successful update or delete uses this. |
| `Results.BadRequest()` / `Results.BadRequest(value)` | 400. The input failed a check. |
| `Results.NotFound()` | 404. |
| `Results.Conflict(value)` | 409. The resource conflicts with current state. |
| `Results.Json(value, statusCode)` | JSON with the status code you pass. |
| `Results.File(stream, contentType)` | A file response. |
| `Results.Unauthorized()` | 401. |
| `Results.StatusCode(code)` | A status with no required body. |

Returning `Results.Ok` from a catch still sends 200. The status code is whatever the `IResult` writes. Callers that branch on the status code need a non-success result for a failure.

### Parameter binding

Minimal API binds handler parameters by type and by name.

| Source | How |
| --- | --- |
| Route | A parameter whose name matches `{id}` in the pattern. |
| Query string | A parameter that is not a route value and is not a complex body, for example `string searchString`. |
| JSON body | `[FromBody]` on a complex type, or a complex type on a POST when the framework infers the body. |
| Services | A parameter whose type is registered in DI. `[FromServices]` makes the source explicit. |
| Header | `[FromHeader(Name = "APIKey")]`. |
| Form | `[FromForm]`, including an `IFormFile`. |
| `HttpContext` | Taken directly when the handler asks for it. |
| `CancellationToken` | Bound to `HttpContext.RequestAborted`. |

`IFormFile` is one uploaded file. `FileName`, `ContentType`, `Length`, and `OpenReadStream()` describe it. The form field name is the name the client put in the multipart part.

### Authentication

| Type | Role |
| --- | --- |
| `AuthenticationBuilder.AddScheme<TOptions, THandler>(name, configure)` | Registers a scheme name and the handler type. |
| `AuthenticationSchemeOptions` | The options object for a scheme that has no extra settings. |
| `AuthenticationOptions.DefaultAuthenticateScheme` | The scheme `UseAuthentication` runs. |
| `DefaultChallengeScheme` | The scheme used when authorization fails and the caller should be challenged. |
| `AuthenticationHandler<TOptions>` | The base class. The constructor receives `IOptionsMonitor<TOptions>`, `ILoggerFactory`, and a `UrlEncoder`. |
| `HandleAuthenticateAsync()` | The method you override. It returns an `AuthenticateResult`. |
| `Request.Headers.TryGetValue(name, out value)` | Reads one header. |
| `AuthenticateResult.Success(ticket)` | Authentication succeeded. |
| `AuthenticateResult.Fail(message)` | Authentication failed. With a fallback policy, the request ends in 401. |
| `AuthenticateResult.NoResult()` | This handler abstains. |

`IOptionsMonitor<T>.CurrentValue` is the current options, and it updates when configuration reloads. The handler base stores the monitor and reads it for you.

### Claims

| Type | Role |
| --- | --- |
| `Claim` | One fact: a type and a value. `ClaimTypes.Name` is the standard name type. |
| `ClaimsIdentity` | A set of claims plus an authentication type. |
| `ClaimsIdentity(claims, authenticationType)` | An identity whose `IsAuthenticated` is true because the authentication type is non-empty. |
| `ClaimsPrincipal` | The user. It holds one or more identities. |
| `AuthenticationTicket` | The principal plus the scheme name that produced it. |
| `HttpContext.User` | The principal for this request, set by the authentication middleware. |

Authorization policies read the principal. A handler that returns success with an unauthenticated identity still fails a policy that requires an authenticated user. The authentication type on the identity is what makes `IsAuthenticated` true.

### Authorization

| Member | Meaning |
| --- | --- |
| `AddAuthorization(configure)` | Registers policies. |
| `AuthorizationOptions.DefaultPolicy` | The policy `[Authorize]` uses when no policy name is given. It requires an authenticated user. |
| `FallbackPolicy` | The policy for endpoints that carry no authorization metadata. |
| `RequireAuthenticatedUser()` | Adds that requirement to a policy. |
| `[Authorize]` | Marks an endpoint or controller as requiring the default policy, or a named policy. |
| `AllowAnonymous` | Marks an endpoint so the fallback policy does not apply. |

Setting `FallbackPolicy` to `DefaultPolicy` requires authentication on every endpoint that does not opt out. Health and OpenAPI endpoints that must stay public are marked `AllowAnonymous`.

In an interview: authentication answers "who is this?". Authorization answers "may this principal do this?". The middleware order follows that sequence.

### OpenAPI

`AddOpenApi()` registers document generation. `MapOpenApi()` adds the endpoint that serves it. The document describes routes, parameters, and the body declared with `Accepts` or inferred from `[FromBody]`. Swagger UI is a separate middleware. The document alone is the JSON contract.

### `DefaultAzureCredential`

`Azure.Identity`. A `TokenCredential` that tries a sequence of sources: environment variables, workload identity, managed identity, Visual Studio, the Azure CLI, and others. `DefaultAzureCredentialOptions` turns individual sources on or off.

`new DefaultAzureCredential(options)` is what startup passes to Key Vault. On Azure App Service the managed identity is the source that succeeds. Locally, a developer login is the source that succeeds. The same code runs in both places.

`AddAzureKeyVault(uri, credential)` pulls secrets into `IConfiguration`. A failure here throws before the app listens, which is intentional: a missing secret store is a failed start.

### Blob storage

| Type | Role |
| --- | --- |
| `BlobServiceClient` | The account. Constructed from a connection string, or from a URI and a `TokenCredential`. |
| `GetBlobContainerClient(name)` | A client for one container. The argument is the container name in the storage account. |
| `BlobContainerClient.CreateIfNotExistsAsync(access)` | Creates the container when it is missing. |
| `PublicAccessType.None` | No anonymous access. Callers present a credential or the connection. |
| `GetBlobClient(blobName)` | A client for one blob. |
| `BlobClient.UploadAsync(stream, options)` | Writes the stream. |
| `BlobUploadOptions` | Headers, metadata, and conditions for the upload. |
| `DownloadStreamingAsync` / `DownloadContentAsync` | Reads a blob back. |

Register `BlobServiceClient` once. It is safe to share. Container and blob clients are cheap wrappers over that service client.

### Document Intelligence

| Type | Role |
| --- | --- |
| `DocumentIntelligenceClient` | The Azure AI Document Intelligence client. |
| `AzureKeyCredential` | A credential that holds an API key for this service. |
| `BinaryData.FromBytes(bytes)` | Wraps a byte array as the payload type the client accepts. |
| `AnalyzeDocumentOptions` | The model id and the content. `"prebuilt-receipt"` is the prebuilt receipt model. `Locale` biases recognition. |
| `AnalyzeDocumentAsync(WaitUntil.Completed, options)` | Starts the analysis and polls until it finishes. |
| `WaitUntil.Completed` | The client waits and returns the finished operation. |
| `WaitUntil.Started` | The client returns as soon as the operation is accepted. The caller polls. |
| `Operation<T>.Value` | The result after completion. |
| `AnalyzeResult.Documents` | The recognized documents. |
| `DocumentField.ValueDate` | A field read as a date. `TryGetValue` on the field dictionary avoids a missing key. |

The prebuilt model returns fields by name. Application code maps those names onto its own result.

## APIs removed from the NevAPI host

Commit `549dbb9` (`Azure KeyVault Implemented & Refactored NevAPI`, 2026-09-28) is the current tip of NevAPI. Its parent, `388e6aa`, is the last host that still called the APIs in this section. JWT issuance arrived in `89db708`. The budget, mileage, and dashboard endpoint classes are still in the NevAPI project. The host no longer registers them, so those URLs are not served.

The project file still references the package `Microsoft.AspNetCore.Authentication.JwtBearer`. The current host never calls it.

### `AddAuthentication` called twice

Each `AddAuthentication(Action<AuthenticationOptions>)` appends a configure delegate. The delegates run in registration order.

| Member | Meaning |
| --- | --- |
| `DefaultAuthenticateScheme` | The scheme `UseAuthentication` runs when this property has a value. |
| `DefaultChallengeScheme` | The scheme authorization uses when it challenges the caller. |
| `DefaultScheme` | Fills whichever of those slots is still empty. `AddAuthentication(string defaultScheme)` sets this property. |
| `JwtBearerDefaults.AuthenticationScheme` | The constant `"Bearer"`. |

The older host set `DefaultAuthenticateScheme` and `DefaultChallengeScheme` to the API-key scheme, then called `AddAuthentication(JwtBearerDefaults.AuthenticationScheme)`. The second call sets `DefaultScheme` to `"Bearer"` and leaves the API-key scheme in `DefaultAuthenticateScheme`. Both handlers are registered. The property that is set chooses which handler runs for a request that does not name a scheme.

In an interview, walk the properties in registration order. `DefaultScheme` fills empty slots. A later assignment to `DefaultScheme` leaves an earlier `DefaultAuthenticateScheme` in place.

### `AddJwtBearer`

`AuthenticationBuilder.AddJwtBearer` registers `JwtBearerHandler` on the `"Bearer"` scheme.

| Member | Meaning |
| --- | --- |
| `JwtBearerOptions.Authority` | Base URL of an OpenID Connect authority. The handler loads `{authority}/.well-known/openid-configuration` and the signing keys that document names. |
| `Audience` | Value the `aud` claim must contain. |
| `TokenValidationParameters` | The explicit checks: issuer, audience, lifetime, and signing key. |
| `RequireHttpsMetadata` | When true, the discovery document is fetched over HTTPS. The default is true. |
| `SaveToken` | Stores the raw token on `AuthenticationProperties` after authentication succeeds. |
| `MapInboundClaims` | When true (the default), short JWT names are rewritten to the long `ClaimTypes` URIs. `role` becomes `ClaimTypes.Role`. |
| `Events` | A `JwtBearerEvents` object. `OnMessageReceived` reads the token from a place other than the `Authorization: Bearer` header. `OnTokenValidated` runs after the checks pass. `OnAuthenticationFailed` runs when validation throws. |

The older options set `Authority` and `Audience` to the API's own base URL and left `TokenValidationParameters.IssuerSigningKey` empty. `Authority` means the signing keys come from that identity provider. A token this API signs itself is checked with `IssuerSigningKey` set to the same key. The older host configured discovery, and the issuer below signed with a local HMAC key.

### Issuing a JWT

The older host mapped `POST api/authenticate`. The delegate read a header, built a token, and returned it as JSON. A missing or rejected header returned `Results.Unauthorized()`.

| Type or member | Meaning |
| --- | --- |
| `Claim` | One fact: a type and a value. The older token carried a name, a role, and an email. |
| `ClaimTypes.Name`, `ClaimTypes.Role`, `ClaimTypes.Email` | The long URIs for those three facts. `[Authorize(Roles = "Admin")]` matches a role claim. |
| `SigningCredentials` | A key plus an algorithm name. |
| `SymmetricSecurityKey(byte[])` | The key for HMAC. `KeySize` is measured in bits. |
| `SecurityAlgorithms.HmacSha256` | The algorithm id `HS256`. HS256 requires a key of at least 256 bits, which is 32 bytes. |
| `Encoding.UTF8.GetBytes` | The bytes of the secret string. Those bytes are the key. |
| `string.PadRight(totalWidth, paddingChar)` | Brings the string up to `totalWidth`. The older issuer used a width of 32 so the UTF-8 key reached 32 bytes. |
| `JwtSecurityToken` | Issuer, audience, claims, `expires`, and `signingCredentials`. `expires` is an absolute UTC time. The older token used `DateTime.UtcNow.AddMinutes(5)`. |
| `JwtSecurityTokenHandler.WriteToken` | Compact serialization: base64url header, payload, and signature, joined by `.`. |
| Outbound claim map | On write, the handler shortens `ClaimTypes.Role` to `role`. With `MapInboundClaims` at its default, the bearer handler maps `role` back to `ClaimTypes.Role` on read. |
| `Results.Ok(new { ResponseToken = token })` | Status 200. The serializer writes the property as `responseToken`. |

`TokenValidationParameters` is the matching half of that issuer. The older bearer options did not set these. An interviewer still expects them for an API that signs its own tokens.

| Property | Meaning |
| --- | --- |
| `ValidateIssuer` / `ValidIssuer` | When validation is on, `iss` must equal `ValidIssuer`. |
| `ValidateAudience` / `ValidAudience` | `aud` must equal `ValidAudience`. |
| `ValidateLifetime` | `nbf` and `exp` are checked. `ClockSkew` allows clock drift. The default skew is five minutes. |
| `ValidateIssuerSigningKey` / `IssuerSigningKey` | The signature must verify with this key. For HS256 that key is a `SymmetricSecurityKey` from the same bytes the issuer used. |

The route carried no `AllowAnonymous()`. `FallbackPolicy` was `DefaultPolicy`, which requires an authenticated user, so authorization ran before the delegate. A token endpoint that must accept a caller who has no credential yet is marked `AllowAnonymous()`, which skips the fallback policy.

The same host called `AddAuthorization()` a second time with no delegate. The authorization services are registered with `TryAdd`, so the second call keeps the first registration. A null configure action leaves `FallbackPolicy` as the first call set it.

### `ISystemClock`

In ASP.NET Core 9, `AuthenticationHandler<TOptions>` is constructed as `(IOptionsMonitor<TOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)`. `ISystemClock.UtcNow` is a `DateTimeOffset`. The interface is obsolete. `TimeProvider` is the replacement.

ASP.NET Core 10 drops the clock parameter. The constructor is `(IOptionsMonitor<TOptions>, ILoggerFactory, UrlEncoder)`. Commit `549dbb9` retargets NevAPI from `net9.0` to `net10.0` and removes `ISystemClock` from the API-key handler, which now calls `base(options, logger, encoder)`.

### `AddDbContext` and the repositories

| Member | Meaning |
| --- | --- |
| `AddDbContext<TContext>(options => options.UseSqlServer())` | Registers `TContext` as scoped: one instance for the request scope. `UseSqlServer()` selects the SQL Server provider. `UseSqlServer(connectionString)` also sets the connection that context opens. |
| `DbContextOptionsBuilder.IsConfigured` | True once a provider is on the options. `OnConfiguring` runs for every new context. The current override returns when this is true, and calls `UseSqlServer` for a context created with the parameterless constructor. |
| `AddTransient<TService, TImplementation>()` | A new instance on every resolve. A minimal-API parameter is resolved once for that request. |

The older host registered `NevDbContext` and the budget, mileage, and dashboard repository interfaces. Each of those delegates takes its repository interface, and each repository takes `NevDbContext`. The current host registers none of them. Putting the routes back requires those registrations: the binder resolves a service parameter from DI and fails the request when the type is absent.

### Endpoint groups the host no longer maps

`MapBudgetMasterEndpoints`, `MapMileageTrackerEndpoints`, and `MapDashboardEndpoints` are still in the project. `Program.cs` no longer calls them. The routes the host still maps are the blob support routes and the receipt parser.

The three groups use the minimal-API members already listed: `MapGet`, `MapPost`, `MapPut`, `MapDelete`, a `{id}` route value, `[FromBody]` for the JSON body, query parameters for search, and `Results.Ok`, `Results.Created`, `Results.NoContent`, `Results.NotFound`, and `Results.BadRequest`.

### Configuration names

`AddAzureKeyVault` copies each secret into `IConfiguration` under the secret's name. The current host reads `KeyVaultUri` from `appsettings.json`, then reads `BlobStorageConnectionString`, `ReceiptParserEndpoint`, and `ReceiptParserKey` from configuration after Key Vault has loaded. The previous `appsettings.json` carried `APIKey`, `AzureStorageConnectionString`, `AzureReceiptParserEndpoint`, and `AzureReceiptParserKey`. `APIUrl` is still read from `appsettings.json`. The API-key handler still reads the configuration entry named `APIKey`.

## EF Core

### `DbContext`, `DbSet`, `DbContextOptions`

`DbContext` is a unit of work and a change tracker around one database connection.

| Member | Meaning |
| --- | --- |
| `DbContextOptions<TContext>` | The immutable options: provider, connection string, and model customizations. |
| `DbContextOptionsBuilder<TContext>` | Builds those options. `Options` returns the built object. |
| `DbSet<T>` | The set of one entity type. It is also an `IQueryable<T>`. |
| `Add(entity)` | Marks the entity `Added`. |
| `Update(entity)` | Marks the entity `Modified`. |
| `Remove(entity)` | Marks the entity `Deleted`. |
| `Entry(entity)` | The `EntityEntry` for one instance. |
| `SaveChangesAsync()` | Writes pending changes in a transaction and returns the number of state entries written. |
| `Dispose()` | Returns the connection to the pool. |

A context is not thread-safe. One operation, one context, then dispose.

### `IDbContextFactory<TContext>`

| Member | Meaning |
| --- | --- |
| `CreateDbContext()` | A new context. The caller disposes it. |
| `CreateDbContextAsync(cancellationToken)` | The async form. |

`AddDbContext<T>()` registers the context as scoped, which fits one scope per HTTP request. A Blazor component lives across many operations, so a scoped context captured by the component would be reused and would overlap work. `AddDbContextFactory<T>()` registers a singleton factory. Each operation does `using var db = await factory.CreateDbContextAsync()`.

The phone registers the factory and then registers `IDbContextFactory<TContext>` again. The last registration is the one that is resolved. That implementation waits until migration has finished, then returns a context. Waiting keeps queries off the file while `Migrate` holds it.

`IDesignTimeDbContextFactory<TContext>.CreateDbContext(string[] args)` is the factory `dotnet ef` uses at design time. The tools call it to build a context when they add or list migrations. The database path inside it is only for that design-time run.

### `DatabaseFacade` and migrations

`DbContext.Database` is a `DatabaseFacade`.

| Member | Meaning |
| --- | --- |
| `Migrate()` | Applies pending migrations. Creates the database when it is missing. |
| `MigrateAsync()` | The async form. |
| `IMigrator.MigrateAsync(target)` | Moves the database to a named migration, including back to an earlier one when the target is older. |
| `GetPendingMigrations()` | Migrations in the assembly that are not in the database history table. |
| `GetAppliedMigrations()` | Migrations recorded in `__EFMigrationsHistory`. |
| `CanConnect()` | True when a connection can be opened. |
| `BeginTransactionAsync()` | Starts a transaction you control. |
| `ExecuteSqlRawAsync(sql, parameters)` | Runs SQL that is not mapped to an entity. |
| `UseSqlite(connectionString, configure)` | Selects the SQLite provider. |
| `UseSqlServer(connectionString)` | Selects the SQL Server provider. |
| `MigrationsAssembly(name)` | The assembly that contains the migration classes, when it is not the assembly of the context startup project. |

`Migrate` compares the history table with the migrations compiled into the assembly and runs the difference. It is safe to call on every start. Shipping a new migration and calling `Migrate` is how a phone installs a schema change.

The connection string is configuration. It includes a password for SQL Server, and this document does not copy it.

### `ModelBuilder`

`OnModelCreating(ModelBuilder modelBuilder)` is where fluent configuration lives.

| Member | Meaning |
| --- | --- |
| `Entity<T>()` | The builder for one entity. |
| `Property(e => e.Date)` | One column. |
| `HasConversion(toProvider, fromProvider)` | A value converter. The first delegate writes the column value. The second reads it back. |
| `HasConversion<int>()` | Stores an enum as its underlying integer. |
| `HasColumnType("TEXT")` | The store type. |
| `HasMaxLength(n)` | The maximum length, used by migrations. |
| `IsRequired()` | The column is non-nullable. |
| `HasIndex(e => new { ... })` | An index on those columns. |
| `IsUnique()` | The index rejects duplicate combinations. |
| `HasKey` | The primary key, when it is not the convention `Id`. |
| `ToTable(name)` | The table name, when it differs from the `DbSet` name. |

Conventions already map `Id` to the primary key and map a `string` to a nullable text column. Fluent configuration is for the decisions conventions cannot see: the `DateOnly` storage format, and the unique combination that defines a duplicate expense or trip.

Changing an enum's numeric values changes the stored integers. The converter `HasConversion<int>()` writes the number, so the declared order of the enum is part of the data contract.

### `IQueryable<T>` and LINQ

`DbSet<T>` implements `IQueryable<T>`. Operators such as `Where`, `Select`, `OrderBy`, `OrderByDescending`, and `Take` add to an expression tree. Nothing hits the database until the query is enumerated.

| Call | What it does |
| --- | --- |
| `Where(predicate)` | Filters. On `IQueryable`, the predicate becomes SQL. |
| `Select(selector)` | Projects. Selecting only the columns you need keeps those columns in the SQL. |
| `OrderBy` / `OrderByDescending` | `ORDER BY`. |
| `Take(n)` | `LIMIT` / `TOP`. |
| `AsNoTracking()` | The results are not tracked. See below. |
| `ToListAsync()` | Runs the query and materializes a list. |
| `FirstOrDefaultAsync()` | The first row, or the default value. |
| `AnyAsync(predicate)` | True when any row matches. Translates to `EXISTS`. |
| `SumAsync()` | The sum. Translates to `SUM`. |
| `CountAsync()` | The count. |
| `EF.Functions.Like(column, pattern)` | SQL `LIKE`. A `%` in the pattern is the wildcard. |

After `ToListAsync`, further LINQ is LINQ to Objects and runs in memory. `Take` before a later in-memory `OrderBy` limits the wrong set. The operators that should run in the database stay on the `IQueryable` before materialization.

`EF.Functions.Like` is a method EF recognizes and translates. A call to an ordinary .NET method inside a predicate often cannot be translated, and EF throws when the query executes.

### Change tracking

The change tracker stores a snapshot of each entity it loaded.

| `EntityState` | Meaning |
| --- | --- |
| `Detached` | The context is not tracking it. |
| `Unchanged` | Loaded and matching the snapshot. |
| `Modified` | A property differs from the snapshot, or `Update` was called. |
| `Added` | Will be inserted. |
| `Deleted` | Will be deleted. |

`AsNoTracking()` returns entities in the `Detached` state and skips the snapshot. Read-only lists and searches use it because the tracker would allocate a snapshot for every row and would never save those rows. A query that loads an entity in order to edit and save it stays tracked, so `SaveChangesAsync` sees the modifications.

`Entry(entity).State = EntityState.Modified` forces every property into the update. Tracked changes write only the properties that differ.

### Save exceptions

| Type | When |
| --- | --- |
| `DbUpdateException` | The database rejected `SaveChanges`. `Entries` lists the entities involved. |
| `DbUpdateConcurrencyException` | A concurrency token did not match. It derives from `DbUpdateException`. |
| `InnerException` | The provider exception. For SQLite that is `SqliteException`. |

A unique index violation surfaces as `DbUpdateException` whose inner `SqliteException` reports a constraint failure. The application catch reads that inner exception and treats the row as a duplicate.

### Transactions and raw SQL

`SaveChangesAsync` already commits its own transaction. `BeginTransactionAsync` is for several operations that must commit or roll back together.

```csharp
using var transaction = await db.Database.BeginTransactionAsync();
await db.Database.ExecuteSqlRawAsync("TRUNCATE TABLE ...");
await db.SaveChangesAsync();
await transaction.CommitAsync();
```

Dispose rolls back a transaction that was not committed. `ExecuteSqlRawAsync` sends the SQL text. Parameters are passed as arguments, and EF parameterizes them. Concatenating a user value into the SQL string is a SQL injection opening. An identifier that cannot be parameterized, such as a column name in a dynamic query, is checked against a fixed allowlist before it is composed.

`TRUNCATE TABLE` is SQL Server syntax. SQLite does not implement that statement. The provider in use decides which SQL is legal.

## DB

### SQLite and SQL Server in EF

| Piece | Phone | API |
| --- | --- | --- |
| Provider | `UseSqlite` | `UseSqlServer` |
| Engine | SQLite, a file | SQL Server |
| Connection | `Filename=` plus the path under `FileSystem.AppDataDirectory` | A connection string from configuration |
| Migrations | Compiled in `NevDBClass`, named with `MigrationsAssembly` | The API context's own configuration |

`UseSqlite` and `UseSqlServer` are the only line that must change for the provider. The `DbContext`, `DbSet`, and LINQ stay the same where the SQL features overlap. Provider-specific SQL belongs behind the provider that understands it.

SQLite types are storage classes: `NULL`, `INTEGER`, `REAL`, `TEXT`, `BLOB`. EF's `DateOnly` converter writes `TEXT` in `yyyy-MM-dd` so ordering and equality match calendar order. A unique index is enforced by SQLite. A failed insert sets the extended error for a constraint violation.

### `SqliteConnection`

`Microsoft.Data.Sqlite`.

| Member | Meaning |
| --- | --- |
| `SqliteConnection` | An open connection to one file. EF opens it for you from the connection string. |
| `ClearAllPools()` | Closes idle pooled connections for this process. |
| `SqliteException.SqliteErrorCode` | The primary SQLite result code. |
| `SqliteExtendedErrorCode` | The more specific code. |
| Code 19 | `SQLITE_CONSTRAINT`. A unique index or a foreign key rejected the write. |

SQLite locks the database file while a connection is open. The connection pool keeps connections open after a context is disposed, ready for the next context. Replacing the file, as a restore does, calls `ClearAllPools` first so the pool drops those handles and the copy can replace the file. New contexts then open the restored file.

### Transactions and durability

SQLite's default journal mode serializes writers. One writer runs at a time, which matches a single-user phone. `Migrate` takes a write lock while it updates the schema. Queries that open the same file wait until that lock is released, which is why context creation waits on the warmup task.

SQL Server uses the connection's isolation level, `READ COMMITTED` by default. A transaction started with `BeginTransactionAsync` holds its locks until `CommitAsync` or rollback. Keep the transaction short. A request that waits on a user, or on another HTTP call, inside a transaction holds locks the whole time.

### Commands you run at design time

These are the EF tools, run against the project that contains the context.

| Command | Effect |
| --- | --- |
| `dotnet ef migrations add Name --project NevDBClass` | Scaffolds a migration from the current model compared with the last snapshot. |
| `dotnet ef migrations remove --project NevDBClass` | Removes the last migration when it has not been applied. |
| `dotnet ef database update --project NevDBClass` | Applies migrations to the design-time database. The running app calls `Database.Migrate` itself, so this is for a machine where you want the file updated from the terminal. |

`--framework net10.0` picks the target framework when the project multi-targets. The design-time factory supplies the context the tools need.
