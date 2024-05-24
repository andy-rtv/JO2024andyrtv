using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JO2024andyrtv.Areas.Identity.Data;
using JO2024andyrtv.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using QRCoder;

public class HomeController : Controller
{
    private readonly JO2024Context _context;
    private readonly UserManager<JO2024User> _userManager;
    private readonly SignInManager<JO2024User> _signInManager;

    // Constructeur pour injecter les d�pendances
    public HomeController(JO2024Context context, UserManager<JO2024User> userManager, SignInManager<JO2024User> signInManager)
    {
        _context = context;
        _userManager = userManager;
        _signInManager = signInManager;
    }

    // Affiche la page d'accueil
    public IActionResult Index()
    {
        return View();
    }

    // Affiche la page Boutique
    public IActionResult Boutique()
    {
        return View();
    }

    // Affiche la page de confidentialit�
    public IActionResult Privacy()
    {
        return View();
    }

    // Affiche la page Billetterie avec des options de filtrage
    public async Task<IActionResult> Billetterie(string searchString, int? genre, decimal? minPrice, decimal? maxPrice, DateTime? startDate, DateTime? endDate, TypeEpreuve? typeEpreuve)
    {
        // Initialisation des filtres
        ViewData["CurrentFilter"] = searchString;
        ViewData["MinPriceFilter"] = minPrice;
        ViewData["MaxPriceFilter"] = maxPrice;
        ViewData["StartDateFilter"] = startDate?.ToString("yyyy-MM-dd");
        ViewData["EndDateFilter"] = endDate?.ToString("yyyy-MM-dd");
        ViewData["TypeEpreuveFilter"] = typeEpreuve;

        var evenements = from e in _context.Evenements select e;

        // Application des filtres
        if (!String.IsNullOrEmpty(searchString))
        {
            evenements = evenements.Where(e => e.SportName.Contains(searchString));
        }

        if (genre.HasValue)
        {
            evenements = evenements.Where(e => e.Genre == (Genre)genre);
        }

        if (minPrice.HasValue)
        {
            evenements = evenements.Where(e => e.Prix >= minPrice.Value);
        }

        if (maxPrice.HasValue)
        {
            evenements = evenements.Where(e => e.Prix <= maxPrice.Value);
        }

        if (startDate.HasValue)
        {
            evenements = evenements.Where(e => e.DateDebut >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            evenements = evenements.Where(e => e.DateFin <= endDate.Value);
        }

        if (typeEpreuve.HasValue)
        {
            evenements = evenements.Where(e => e.TypeEpreuve == typeEpreuve);
        }

        return View(await evenements.AsNoTracking().ToListAsync());
    }

    // Affiche les d�tails d'un �v�nement
    public async Task<IActionResult> EventDetails(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var evenement = await _context.Evenements
            .FirstOrDefaultAsync(m => m.Id_Evenement == id);
        if (evenement == null)
        {
            return NotFound();
        }

        ViewBag.Offres = await _context.Offres.ToListAsync();
        return View(evenement);
    }

    // Ajoute un �v�nement au panier
    [HttpPost]
    public IActionResult AddToCart(int id, int quantity, int offreId)
    {
        var evenement = _context.Evenements.FirstOrDefault(e => e.Id_Evenement == id);
        if (evenement == null)
        {
            return NotFound();
        }

        var offre = _context.Offres.FirstOrDefault(o => o.Id_Offre == offreId);
        if (offre == null)
        {
            return NotFound();
        }

        List<PanierItem> cart = HttpContext.Session.Get<List<PanierItem>>("Cart") ?? new List<PanierItem>();
        var cartItem = cart.FirstOrDefault(ci => ci.EvenementId == id && ci.OffreId == offreId);

        if (cartItem == null)
        {
            cart.Add(new PanierItem
            {
                EvenementId = id,
                Evenement = evenement,
                Quantity = quantity,
                OffreId = offreId,
                Offre = offre
            });
        }
        else
        {
            cartItem.Quantity += quantity;
        }

        HttpContext.Session.Set("Cart", cart);

        return RedirectToAction("Billetterie");
    }

    // Affiche le contenu du panier
    public IActionResult Cart()
    {
        var cart = HttpContext.Session.Get<List<PanierItem>>("Cart") ?? new List<PanierItem>();
        return View(cart);
    }

    // Supprime un �l�ment du panier
    [HttpPost]
    public IActionResult RemoveFromCart(int evenementId, int offreId)
    {
        var cart = HttpContext.Session.Get<List<PanierItem>>("Cart") ?? new List<PanierItem>();
        var cartItem = cart.FirstOrDefault(ci => ci.EvenementId == evenementId && ci.OffreId == offreId);

        if (cartItem != null)
        {
            cart.Remove(cartItem);
            HttpContext.Session.Set("Cart", cart);
        }

        return RedirectToAction("Cart");
    }

    // Met � jour la quantit� d'un �l�ment dans le panier
    [HttpPost]
    public IActionResult UpdateQuantity(int evenementId, int offreId, int quantity)
    {
        var cart = HttpContext.Session.Get<List<PanierItem>>("Cart") ?? new List<PanierItem>();
        var cartItem = cart.FirstOrDefault(ci => ci.EvenementId == evenementId && ci.OffreId == offreId);

        if (cartItem != null)
        {
            cartItem.Quantity = quantity;
            HttpContext.Session.Set("Cart", cart);
        }

        return RedirectToAction("Cart");
    }

    // Affiche la page de confirmation d'achat
    public IActionResult ConfirmerAchat()
    {
        var cart = HttpContext.Session.Get<List<PanierItem>>("Cart") ?? new List<PanierItem>();
        return View(cart);
    }

    // Sauvegarde le panier avant la connexion
    [HttpPost]
    public async Task<IActionResult> SaveCartBeforeLogin()
    {
        var cart = HttpContext.Session.Get<List<PanierItem>>("Cart");
        if (cart != null)
        {
            TempData["Cart"] = JsonConvert.SerializeObject(cart);
        }
        return RedirectToPage("/Account/Login", new { area = "Identity", returnUrl = Url.Action("LoadCartAfterLogin") });
    }

    // Charge le panier apr�s la connexion
    [HttpGet]
    public IActionResult LoadCartAfterLogin()
    {
        if (TempData.TryGetValue("Cart", out var cartData))
        {
            var cart = JsonConvert.DeserializeObject<List<PanierItem>>(cartData.ToString());
            HttpContext.Session.Set("Cart", System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(cart)));
        }
        return RedirectToAction("ConfirmerAchat");
    }

    // Confirme l'achat des �l�ments dans le panier
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> ConfirmerAchatPost()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("ConfirmerAchatPost") });
        }

        var cart = HttpContext.Session.Get<List<PanierItem>>("Cart");
        if (cart == null || cart.Count == 0)
        {
            return RedirectToAction("Cart");
        }

        var achat = new Achat
        {
            DateAchat = DateTime.UtcNow,
            Id_Utilisateur = user.Id,
            MontantTotal = cart.Sum(item => item.Quantity * item.Evenement.Prix * (1 - item.Offre.Pourcentage / 100m))
        };

        _context.Achats.Add(achat);
        await _context.SaveChangesAsync();

        foreach (var item in cart)
        {
            var achatEvenementOffre = new AchatEvenementOffre
            {
                AchatId = achat.Id_Achat,
                EvenementId = item.EvenementId,
                OffreId = item.OffreId
            };

            _context.AchatEvenementOffres.Add(achatEvenementOffre);
            await _context.SaveChangesAsync();

            for (int i = 0; i < item.Quantity; i++)
            {
                var ticket = new Ticket
                {
                    CreationDate = DateTime.UtcNow,
                    Id_Utilisateur = user.Id,
                    Id_AchatEvenementOffre = achatEvenementOffre.Id_AchatEvenementOffre,
                    TicketGuid = Guid.NewGuid()
                };

                // G�n�rer le QRCode et obtenir le chemin
                ticket.QRCodePath = GenerateQRCode(achat, ticket);

                _context.Tickets.Add(ticket);
            }
        }

        await _context.SaveChangesAsync();

        // Vider le panier apr�s confirmation de l'achat
        HttpContext.Session.Remove("Cart");

        return RedirectToAction("Index");
    }

    // G�n�re un QRCode pour le ticket et retourne le chemin de l'image g�n�r�e
    private string GenerateQRCode(Achat achat, Ticket ticket)
    {
        using (var qrGenerator = new QRCodeGenerator())
        {
            string baseUrl = "https://votre-domaine.com/Admin/ValidateTicket"; // Remplacez par votre domaine
            string qrContent = $"{baseUrl}?achatGuid={achat.AchatGuid}&ticketGuid={ticket.TicketGuid}";
            var qrCodeData = qrGenerator.CreateQrCode(qrContent, QRCodeGenerator.ECCLevel.Q);
            var qrCode = new PngByteQRCode(qrCodeData);
            byte[] qrCodeImage = qrCode.GetGraphic(20);

            var fileName = $"{Guid.NewGuid()}.png";
            var filePath = Path.Combine("wwwroot", "qrcodes", fileName);
            System.IO.File.WriteAllBytes(filePath, qrCodeImage);

            return "\\" + Path.Combine("qrcodes", fileName); // Return relative path for storing in database with backslashes
        }
    }

    // Redirige correctement apr�s la connexion
    [HttpGet]
    public IActionResult RedirectAfterLogin(string returnUrl)
    {
        if (Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }
        return RedirectToAction("Index", "Home");
    }
}
