using AINewsEngine.Data;
using AINewsEngine.DTOs;
using AINewsEngine.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;


[Route("api/[controller]")]
[ApiController]
public class HaberlerController : ControllerBase
{
    private readonly VeritabaniContext _context;

    public HaberlerController(VeritabaniContext context)
    {
        _context = context;
    }

    // PANEL API - GET: api/Haberler?page=1&pageSize=10&kategoriId=1
    // Admin paneli i�in - T�M haberleri g�sterir (onaylanmam�� dahil)
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    [HttpGet]
    public async Task<ActionResult<object>> GetHaberler(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] int? kategoriId = null)
    {
        // Sayfa numaras� 1'den ba�lamal�
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100; // Maksimum limit

        // Temel sorgu: T�M haberleri al (admin paneli i�in onaylanmam�� olanlar da g�r�nmeli)
        IQueryable<Haber> query = _context.Haberler.AsNoTracking();

        // Kategori filtresi varsa uygula
        if (kategoriId.HasValue && kategoriId.Value > 0)
        {
            query = query.Where(h => h.KategoriId == kategoriId.Value);
        }

        // Toplam kay�t say�s�
        var totalCount = await query.CountAsync();

        // Sayfalama ile veri �ekme
        var haberler = await query
            .OrderByDescending(h => h.YayinTarihi) // En yeni haberler �nce
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new
        {
            data = haberler,
            totalCount = totalCount
        });
    }

    // PANEL API - GET: api/Haberler/search?term=kelime&page=1&pageSize=10
    // Admin paneli i�in arama
    [HttpGet("search")]
    public async Task<ActionResult<object>> SearchHaberler(
        [FromQuery] string term,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        // Sayfa numaras� 1'den ba�lamal�
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100; // Maksimum limit

        // Temel sorgu: T�M haberleri al (admin paneli i�in)
        IQueryable<Haber> query = _context.Haberler.AsNoTracking();

        // Arama terimi varsa filtrele
        if (!string.IsNullOrWhiteSpace(term))
        {
            term = term.Trim().ToLower();
            query = query.Where(h =>
                h.Baslik.ToLower().Contains(term) ||
                h.Icerik.ToLower().Contains(term)
            );
        }

        // Toplam kay�t say�s� (filtrelenmi�)
        var totalCount = await query.CountAsync();

        // Sayfalama ile veri �ekme
        var haberler = await query
            .OrderByDescending(h => h.YayinTarihi) // En yeni haberler �nce
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new
        {
            data = haberler,
            totalCount = totalCount
        });
    }

    // FLUTTER API - GET: api/Haberler/paged?pageNumber=1&pageSize=10&kategoriId=1
    // Flutter i�in - SADECE onaylanm�� haberleri g�sterir
    [HttpGet("paged")]
    public async Task<ActionResult<PagedResult<Haber>>> GetPagedHaberler(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] int? kategoriId = null)
    {
        // Temel sorgu: Sadece onaylanm�� haberleri al (Flutter i�in)
        var query = _context.Haberler
                            .Where(h => h.Onaylandi == true)
                            .AsNoTracking();
                           
        // Kategori filtresi varsa uygula
        if (kategoriId.HasValue && kategoriId.Value != 0)
        {
            query = query.Where(h => h.KategoriId == kategoriId.Value);
        }

        // S�ralamay� filtrelemeden sonra yap�yoruz
        query = query.OrderByDescending(h => h.YayinTarihi);

        // Toplam haber say�s�n� al�yoruz
        var totalItems = await query.CountAsync();

        // Veritaban�ndan sadece ilgili sayfadaki haberleri �ekiyoruz
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();


        foreach (var haber in items)
        {
            if (!string.IsNullOrEmpty(haber.ResimYolu))
            {
                // �rnek: "/images/haber1.jpg"
                haber.ResimYolu = $"/images/{haber.ResimYolu}";
            }
        }

        // Sayfa bilgilerini hesapl�yoruz
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        var paginationInfo = new PaginationInfo
        {
            TotalItems = totalItems,
            PageSize = pageSize,
            PageNumber = pageNumber,
            TotalPages = totalPages
        };

        // Sayfalanm�� sonucu olu�turup d�nd�r�yoruz
        var pagedResult = new PagedResult<Haber>
        {
            Items = items,
            Pagination = paginationInfo
        };

        return Ok(pagedResult);
    }

    // PANEL API - Haber onaylama endpoint
    [Authorize(Roles = "Admin,Moderator")]
    [HttpPut("{id}/approve")]
    public async Task<ActionResult<Haber>> ApproveHaber(int id)
    {
        var haber = await _context.Haberler.FindAsync(id);
        if (haber == null)
        {
            return NotFound();
        }

        haber.Onaylandi = true;
        await _context.SaveChangesAsync();

        return Ok(haber);
    }

    // PANEL API - Okunma say�s�n� art�rma
    [HttpPut("{id}/increment-read")]
    public async Task<ActionResult> IncrementReadCount(int id)
    {
        var haber = await _context.Haberler.FindAsync(id);
        if (haber == null)
        {
            return NotFound();
        }

        haber.OkunmaSayisi++;
        await _context.SaveChangesAsync();

        return Ok();
    }

    // PANEL API - T�klanma say�s�n� art�rma  
    [HttpPut("{id}/increment-click")]
    public async Task<ActionResult> IncrementClickCount(int id)
    {
        var haber = await _context.Haberler.FindAsync(id);
        if (haber == null)
        {
            return NotFound();
        }

        haber.TiklanmaSayisi++;
        await _context.SaveChangesAsync();

        return Ok();
    }

    // FLUTTER API - GET: api/Haberler/5
    // ID'ye g�re tek bir haber getirir
    [HttpGet("{id}")]
    public async Task<ActionResult<Haber>> GetHaber(int id)
    {
        var haber = await _context.Haberler
                                  .AsNoTracking()
                                  .FirstOrDefaultAsync(h => h.Id == id);

        if (haber == null)
        {
            return NotFound();
        }

        return haber;
    }

    // FLUTTER API - PUT: api/Haberler/5
    // Mevcut bir haberi g�nceller
    [Authorize(Roles = "Admin,Moderator")]
    [HttpPut("{id}")]
    public async Task<IActionResult> PutHaber(int id, Haber haber)
    {
        if (id != haber.Id)
        {
            return BadRequest();
        }

        _context.Entry(haber).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!HaberExists(id))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }

        return NoContent();
    }

    // FLUTTER API - POST: api/Haberler
    // Yeni bir haber olu�turur
    [Authorize(Roles = "Admin,Moderator")]
    [HttpPost]
    public async Task<ActionResult<Haber>> PostHaber(Haber haber)
    {
        _context.Haberler.Add(haber);
        await _context.SaveChangesAsync();

        // Olu�turulan kayna��n konumunu header'da d�nd�r�r.
        return CreatedAtAction("GetHaber", new { id = haber.Id }, haber);
    }

    // FLUTTER API - DELETE: api/Haberler/5
    // Bir haberi siler
    [Authorize(Roles = "Admin,Moderator")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteHaber(int id)
    {
        var haber = await _context.Haberler.FindAsync(id);
        if (haber == null)
        {
            return NotFound();
        }

        _context.Haberler.Remove(haber);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // FLUTTER API - T�klanma sayac�n� art�rma
    [HttpPost("{id}/tiklandi")]
    public async Task<IActionResult> TiklanmaArtir(int id)
    {
        var haber = await _context.Haberler.FindAsync(id);
        if (haber == null)
        {
            return NotFound();
        }

        haber.TiklanmaSayisi++;
        await _context.SaveChangesAsync();

        return Ok();
    }

    // FLUTTER API - Okunma sayac�n� art�rma
    [HttpPost("{id}/okundu")]
    public async Task<IActionResult> OkunmaArtir(int id)
    {
        var haber = await _context.Haberler.FindAsync(id);
        if (haber == null)
        {
            return NotFound();
        }

        haber.OkunmaSayisi++;
        await _context.SaveChangesAsync();

        return Ok();
    }

    // FLUTTER API - Haber onaylama (yetkilendirmeli)
    [Authorize(Roles = "Admin,Moderator")]
    [HttpPost("{id}/onayla")]
    public async Task<IActionResult> Onayla(int id)
    {
        var haber = await _context.Haberler.FindAsync(id);
        if (haber == null)
        {
            return NotFound();
        }

        haber.Onaylandi = true;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Haber ba�ar�yla onayland�." });
    }

    // FLUTTER API - En �ok okunan haberleri getir
    [HttpGet("most-read")]
    public async Task<ActionResult<List<Haber>>> GetMostReadNews()
    {
        var mostReadNews = await _context.Haberler
            .Where(h => h.Onaylandi == true)
            .OrderByDescending(h => h.OkunmaSayisi)
            .Take(10)
            .AsNoTracking()
            .ToListAsync();

        return Ok(mostReadNews);
    }

    // FLUTTER API - En �ok t�klanan haberleri getir
    [HttpGet("most-clicked")]
    public async Task<ActionResult<List<Haber>>> GetMostClickedNews()
    {
        var mostClickedNews = await _context.Haberler
            .Where(h => h.Onaylandi == true)
            .OrderByDescending(h => h.TiklanmaSayisi)
            .Take(10)
            .AsNoTracking()
            .ToListAsync();

        return Ok(mostClickedNews);
    }





    [HttpGet("GetTop5ReadNews")]
    public async Task<IActionResult> GetTOP5ReadNews() // Metod adını da GetMostReadNews olarak değiştirdim
    {
        return Ok(await _context.Haberler.OrderByDescending(h => h.OkunmaSayisi).Take(5).ToListAsync());
    }


   
    
    [HttpGet("GetTop5ClickedNews")]
    public async Task<IActionResult> GetTOP5ClickedNews() // Metod adını da GetMostClickedNews olarak değiştirdim
    {
        return Ok(await _context.Haberler.OrderByDescending(h => h.TiklanmaSayisi).Take(5).ToListAsync());
    }

    //// === En Çok Tıklanan Haberler Endpoint'i ===
    //// Tam URL: api/News/most-clicked-news
    //[HttpGet("most-clicked-news")]
    //public async Task<IActionResult> GetMostClickedNews() // Metod adını da GetMostClickedNews olarak değiştirdim
    //{
    //    return Ok(await _context.Haberler.OrderByDescending(h => h.TiklanmaSayisi).Take(5).ToListAsync());
    //}




    // Yardımcı metod
    private bool HaberExists(int id)
    {
        return _context.Haberler.Any(e => e.Id == id);
    }
}
