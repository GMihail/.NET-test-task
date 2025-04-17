using Microsoft.AspNetCore.Mvc;
using Shop.Models;
using Shop.Services;
using Supabase;

public class ProductsController : Controller
{
    private readonly SupabaseService _supabase;

    public ProductsController(SupabaseService supabase)
        => _supabase = supabase;

    public async Task<IActionResult> Index()
    {
        var products = await _supabase.Client
            .From<Product>()
            .Get();

        return View(products.Models);
    }

}