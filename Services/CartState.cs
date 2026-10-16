using System.Security.Claims;
using System.Security.Cryptography;
using JardinNoemi.DTOs;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace JardinNoemi.Services;

/// <summary>
/// Estado del carrito para el circuito interactivo actual (servicio Scoped = uno por pestaña).
/// La fuente de verdad es la base de datos; en el navegador solo se guarda, cifrado y firmado
/// con Data Protection, el identificador del carrito anónimo.
/// </summary>
public sealed class CartState(
    ICartService cartService,
    ProtectedLocalStorage storage,
    AuthenticationStateProvider authStateProvider,
    ILogger<CartState> logger)
{
    private const string StorageKey = "jn.cart";

    private Task? _initialization;
    private Guid? _cartId;
    private string? _userId;

    public CartDto Cart { get; private set; } = CartDto.Empty;
    public bool IsInitialized { get; private set; }
    public bool IsDrawerOpen { get; private set; }
    public bool IsBusy { get; private set; }

    /// <summary>Se dispara ante cualquier cambio de carrito o del panel lateral.</summary>
    public event Action? Changed;

    /// <summary>
    /// Debe llamarse desde OnAfterRenderAsync (requiere JS interop para leer el almacenamiento del navegador).
    /// Es idempotente: varias llamadas comparten la misma inicialización.
    /// </summary>
    public Task EnsureInitializedAsync() => _initialization ??= InitializeCoreAsync();

    private async Task InitializeCoreAsync()
    {
        try
        {
            var auth = await authStateProvider.GetAuthenticationStateAsync();
            _userId = auth.User.Identity?.IsAuthenticated == true
                ? auth.User.FindFirstValue(ClaimTypes.NameIdentifier)
                : null;

            Guid? localId = null;
            try
            {
                var stored = await storage.GetAsync<Guid>(StorageKey);
                if (stored.Success) localId = stored.Value;
            }
            catch (CryptographicException)
            {
                // Valor manipulado o claves rotadas: se descarta.
                await storage.DeleteAsync(StorageKey);
            }

            var resolution = await cartService.ResolveCartAsync(localId, _userId);
            _cartId = resolution.CartId;

            if (_userId is not null && resolution.AnonymousCartConsumed)
            {
                await storage.DeleteAsync(StorageKey);
            }
            else if (_userId is null && localId is not null && _cartId is null)
            {
                await storage.DeleteAsync(StorageKey);
            }

            Cart = await cartService.GetCartAsync(_cartId);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "No se pudo inicializar el carrito.");
            Cart = CartDto.Empty;
        }
        finally
        {
            IsInitialized = true;
            Notify();
        }
    }

    public async Task<CartResult> AddAsync(int productId, int quantity = 1, bool openDrawer = true)
    {
        await EnsureInitializedAsync();
        return await RunAsync(async () =>
        {
            if (_cartId is null)
            {
                _cartId = await cartService.CreateCartAsync(_userId);
                if (_userId is null)
                {
                    await storage.SetAsync(StorageKey, _cartId.Value);
                }
            }

            var result = await cartService.AddItemAsync(_cartId.Value, productId, quantity);
            if (result.Success && openDrawer) IsDrawerOpen = true;
            return result;
        });
    }

    public async Task<CartResult> SetQuantityAsync(int productId, int quantity)
    {
        await EnsureInitializedAsync();
        if (_cartId is null) return CartResult.Fail(Cart, "Tu carrito está vacío.");
        return await RunAsync(() => cartService.SetQuantityAsync(_cartId.Value, productId, quantity));
    }

    public async Task<CartResult> RemoveAsync(int productId)
    {
        await EnsureInitializedAsync();
        if (_cartId is null) return CartResult.Ok(Cart);
        return await RunAsync(() => cartService.RemoveItemAsync(_cartId.Value, productId));
    }

    public async Task RefreshAsync()
    {
        await EnsureInitializedAsync();
        Cart = await cartService.GetCartAsync(_cartId);
        Notify();
    }

    /// <summary>Tras un pedido exitoso el servidor ya vació el carrito; aquí solo se sincroniza la UI.</summary>
    public void MarkAsEmptied()
    {
        Cart = CartDto.Empty;
        Notify();
    }

    public Guid? CartId => _cartId;

    public void OpenDrawer()
    {
        IsDrawerOpen = true;
        Notify();
    }

    public void CloseDrawer()
    {
        if (!IsDrawerOpen) return;
        IsDrawerOpen = false;
        Notify();
    }

    private async Task<CartResult> RunAsync(Func<Task<CartResult>> operation)
    {
        IsBusy = true;
        Notify();
        try
        {
            var result = await operation();
            Cart = result.Cart;
            return result;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Error al actualizar el carrito.");
            return CartResult.Fail(Cart, "No pudimos actualizar tu carrito. Inténtalo nuevamente.");
        }
        finally
        {
            IsBusy = false;
            Notify();
        }
    }

    private void Notify() => Changed?.Invoke();
}
