using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using DartSmartNet.Server.Application.DTOs.Tournament;
using DartSmartNet.Server.Application.Services;
using DartSmartNet.Server.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DartSmartNet.Server.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TournamentController : ControllerBase
{
    private readonly ITournamentService _tournamentService;
    private readonly ILogger<TournamentController> _logger;

    public TournamentController(ITournamentService tournamentService, ILogger<TournamentController> logger)
    {
        _tournamentService = tournamentService;
        _logger = logger;
    }

    /// <summary>
    /// Get public tournaments
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetPublicTournaments(
        [FromQuery] TournamentStatus? status = null,
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var tournaments = await _tournamentService.GetPublicTournamentsAsync(status, limit, cancellationToken);
        return Ok(tournaments);
    }

    /// <summary>
    /// Get tournaments I'm participating in
    /// </summary>
    [HttpGet("my-tournaments")]
    public async Task<IActionResult> GetMyTournaments(
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();
        var tournaments = await _tournamentService.GetMyTournamentsAsync(userId, limit, cancellationToken);
        return Ok(tournaments);
    }

    /// <summary>
    /// Get tournaments I've organized
    /// </summary>
    [HttpGet("organized")]
    public async Task<IActionResult> GetOrganizedTournaments(CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();
        var tournaments = await _tournamentService.GetOrganizedTournamentsAsync(userId, cancellationToken);
        return Ok(tournaments);
    }

    /// <summary>
    /// Get tournament details by ID
    /// </summary>
    [HttpGet("{tournamentId:guid}")]
    public async Task<IActionResult> GetTournament(Guid tournamentId, CancellationToken cancellationToken)
    {
        var tournament = await _tournamentService.GetTournamentAsync(tournamentId, cancellationToken);
        if (tournament == null)
            return NotFound(new { message = "Tournament not found" });

        return Ok(tournament);
    }

    /// <summary>
    /// Create a new tournament
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateTournament(
        [FromBody] CreateTournamentRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetUserId();
            var tournament = await _tournamentService.CreateTournamentAsync(userId, request, cancellationToken);
            return CreatedAtAction(nameof(GetTournament), new { tournamentId = tournament.Id }, tournament);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating tournament");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Update tournament details
    /// </summary>
    [HttpPut("{tournamentId:guid}")]
    public async Task<IActionResult> UpdateTournament(
        Guid tournamentId,
        [FromBody] UpdateTournamentRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetUserId();
            var tournament = await _tournamentService.UpdateTournamentAsync(userId, tournamentId, request, cancellationToken);
            if (tournament == null)
                return NotFound(new { message = "Tournament not found or access denied" });

            return Ok(tournament);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Delete a tournament
    /// </summary>
    [HttpDelete("{tournamentId:guid}")]
    public async Task<IActionResult> DeleteTournament(Guid tournamentId, CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetUserId();
            var success = await _tournamentService.DeleteTournamentAsync(userId, tournamentId, cancellationToken);
            if (!success)
                return NotFound(new { message = "Tournament not found or access denied" });

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Join a tournament
    /// </summary>
    [HttpPost("{tournamentId:guid}/join")]
    public async Task<IActionResult> JoinTournament(
        Guid tournamentId,
        [FromBody] JoinTournamentRequest? request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetUserId();
            var participant = await _tournamentService.JoinTournamentAsync(
                userId, tournamentId, request?.JoinCode, cancellationToken);
            return Ok(participant);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Leave a tournament
    /// </summary>
    [HttpPost("{tournamentId:guid}/leave")]
    public async Task<IActionResult> LeaveTournament(Guid tournamentId, CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetUserId();
            var success = await _tournamentService.LeaveTournamentAsync(userId, tournamentId, cancellationToken);
            if (!success)
                return NotFound(new { message = "Not participating in this tournament" });

            return Ok(new { message = "Left tournament successfully" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get tournament participants
    /// </summary>
    [HttpGet("{tournamentId:guid}/participants")]
    public async Task<IActionResult> GetParticipants(Guid tournamentId, CancellationToken cancellationToken)
    {
        var participants = await _tournamentService.GetParticipantsAsync(tournamentId, cancellationToken);
        return Ok(participants);
    }

    /// <summary>
    /// Start the tournament (organizer only)
    /// </summary>
    [HttpPost("{tournamentId:guid}/start")]
    public async Task<IActionResult> StartTournament(Guid tournamentId, CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetUserId();
            var tournament = await _tournamentService.StartTournamentAsync(userId, tournamentId, cancellationToken);
            return Ok(tournament);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Cancel the tournament (organizer only)
    /// </summary>
    [HttpPost("{tournamentId:guid}/cancel")]
    public async Task<IActionResult> CancelTournament(Guid tournamentId, CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetUserId();
            var tournament = await _tournamentService.CancelTournamentAsync(userId, tournamentId, cancellationToken);
            return Ok(tournament);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get tournament bracket
    /// </summary>
    [HttpGet("{tournamentId:guid}/bracket")]
    [AllowAnonymous]
    public async Task<IActionResult> GetBracket(Guid tournamentId, CancellationToken cancellationToken)
    {
        try
        {
            var bracket = await _tournamentService.GetBracketAsync(tournamentId, cancellationToken);
            return Ok(bracket);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get my pending matches across all tournaments
    /// </summary>
    [HttpGet("matches/pending")]
    public async Task<IActionResult> GetPendingMatches(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var matches = await _tournamentService.GetPendingMatchesAsync(userId, cancellationToken);
        return Ok(matches);
    }

    /// <summary>
    /// Get match details
    /// </summary>
    [HttpGet("matches/{matchId:guid}")]
    public async Task<IActionResult> GetMatch(Guid matchId, CancellationToken cancellationToken)
    {
        var match = await _tournamentService.GetMatchAsync(matchId, cancellationToken);
        if (match == null)
            return NotFound(new { message = "Match not found" });

        return Ok(match);
    }

    /// <summary>
    /// Start a match
    /// </summary>
    [HttpPost("matches/{matchId:guid}/start")]
    public async Task<IActionResult> StartMatch(Guid matchId, CancellationToken cancellationToken)
    {
        try
        {
            var match = await _tournamentService.StartMatchAsync(matchId, cancellationToken);
            return Ok(match);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Complete a match with result
    /// </summary>
    [HttpPost("matches/{matchId:guid}/complete")]
    public async Task<IActionResult> CompleteMatch(
        Guid matchId,
        [FromBody] CompleteMatchRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var match = await _tournamentService.CompleteMatchAsync(
                matchId, request.WinnerId, request.Player1Legs, request.Player2Legs, cancellationToken);
            return Ok(match);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;

        if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("User ID not found in token");
        }

        return userId;
    }
}

public record CompleteMatchRequest(Guid WinnerId, int Player1Legs, int Player2Legs);
