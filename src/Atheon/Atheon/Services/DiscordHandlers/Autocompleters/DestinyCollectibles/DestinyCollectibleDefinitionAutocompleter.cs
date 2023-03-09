﻿using Atheon.Services.BungieApi;
using Atheon.Services.Interfaces;
using Discord;
using Discord.Interactions;
using DotNetBungieAPI.Extensions;
using DotNetBungieAPI.Models.Destiny.Definitions.Collectibles;
using System.Linq;

namespace Atheon.Services.DiscordHandlers.Autocompleters.DestinyCollectibles;

public class DestinyCollectibleDefinitionAutocompleter : AutocompleteHandler
{
    private readonly IBungieClientProvider _bungieClientProvider;
    private readonly ILogger<DestinyCollectibleDefinitionAutocompleter> _logger;
    private readonly DestinyDefinitionDataService _destinyDefinitionDataService;

    public DestinyCollectibleDefinitionAutocompleter(
        IBungieClientProvider bungieClientProvider,
        ILogger<DestinyCollectibleDefinitionAutocompleter> logger,
        DestinyDefinitionDataService destinyDefinitionDataService)
    {
        _bungieClientProvider = bungieClientProvider;
        _logger = logger;
        _destinyDefinitionDataService = destinyDefinitionDataService;
    }
    public override async Task<AutocompletionResult> GenerateSuggestionsAsync(
        IInteractionContext context,
        IAutocompleteInteraction autocompleteInteraction,
        IParameterInfo parameter,
        IServiceProvider services)
    {
        try
        {
            var client = await _bungieClientProvider.GetClientAsync();
            var searchEntry = (string)autocompleteInteraction.Data.Options.First(x => x.Focused).Value;

            var searchResults = client
                .Repository
                .GetAll<DestinyCollectibleDefinition>()
                .Where(x => x.DisplayProperties.Name.Contains(searchEntry, StringComparison.InvariantCultureIgnoreCase))
                .Take(20);

            var results = searchResults
                .Where(x => x.DisplayProperties.Name.Length > 0)
                .Select(x => new AutocompleteResult(GetCollectibleDisplayName(x), x.Hash.ToString()));

            return !results.Any() ? AutocompletionResult.FromSuccess() : AutocompletionResult.FromSuccess(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to form collectibles for query");
            return AutocompletionResult.FromSuccess();
        }
    }

    private string GetCollectibleDisplayName(DestinyCollectibleDefinition destinyCollectible)
    {
        var (name, _) = _destinyDefinitionDataService.GetCollectibleDisplayProperties(destinyCollectible);
        return new string($"{name} ({destinyCollectible.Item.Select(x => x?.ItemTypeAndTierDisplayName)})".Take(100).ToArray());
    }
}
