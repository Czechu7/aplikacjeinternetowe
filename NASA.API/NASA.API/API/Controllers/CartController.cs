using API.Data;
using API.DTOs;
using API.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    public class CartController(DataContext context) : BaseApiController
    {
        [AllowAnonymous]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Cart>>> GetCarts()
        {
            var carts = await context.Carts
                .Include(c => c.User)
                .Include(c => c.Product)
                .ToListAsync();

            return carts;
        }

        [AllowAnonymous]
        [HttpGet("{id}")]
        public async Task<ActionResult<Cart>> GetCart(int id)
        {
            var cart = await context.Carts
                .Include(c => c.User)
                .Include(c => c.Product)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (cart == null) return NotFound();

            return cart;
        }

        [HttpPost]
        public async Task<ActionResult<Cart>> CreateCart(CartDto cartDto)
        {
            // Verify if user exists
            var user = await context.Users.FindAsync(cartDto.UserId);
            if (user == null) return BadRequest("User not found");

            // Verify if product exists
            var product = await context.Products.FindAsync(cartDto.ProductId);
            if (product == null) return BadRequest("Product not found");

            var cart = new Cart
            {
                UserId = cartDto.UserId,
                ProductId = cartDto.ProductId
            };

            context.Carts.Add(cart);
            await context.SaveChangesAsync();

            // Reload cart with included entities
            cart = await context.Carts
                .Include(c => c.User)
                .Include(c => c.Product)
                .FirstOrDefaultAsync(c => c.Id == cart.Id);

            return cart;
        }
    }
}