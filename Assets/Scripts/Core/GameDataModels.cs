using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

#region Server Communication Models

[Serializable]
public class InitData
{
    public string id = "initData";
    public ServerGameData gameData;
    public ServerFeatures features;
    public ServerUIData uiData;
    public ServerPlayer player;
}

[Serializable]
public class ServerGameData
{
    public List<List<int>> lines;
    public List<double> bets;
    public int totalLines;
}

[Serializable]
public class ServerFeatures
{
    public FreeSpinFeature freeSpins;
    public BuyFeature buyFeature;
    public int betMultiplier;
    public int maxWinMultiplier;
    public int minWinMultiplier;
}

[Serializable]
public class FreeSpinFeature
{
    public bool enabled;
    public int initialSpins;
    public bool stickyWilds;
    public bool wildMultiplierPersist;
    public OverlayScatterFeature overlayScatter;
}

[Serializable]
public class OverlayScatterFeature
{
    public bool enabled;
    public List<int> values;
    public ExtraSpinsData extraSpins;
}

[Serializable]
public class ExtraSpinsData
{
    [JsonProperty("2")] public int _2; // For 2 scatters
    [JsonProperty("3")] public int _3; // For 3 scatters
    [JsonProperty("4")] public int _4; // For 4 scatters
    [JsonProperty("5")] public int _5; // For 5 scatters
}

[Serializable]
public class BuyFeature
{
    public bool enabled;
    public double costMultiplier;
}

[Serializable]
public class ServerUIData
{
    public PaylineData paylines;
}

[Serializable]
public class PaylineData
{
    public List<ServerSymbolInfo> symbols;
}

[Serializable]
public class ServerSymbolInfo
{
    public int id;
    public string name;
    public List<double> multiplier; // Note: "multiplier" not "multipliers"
    public double payout;
}

[Serializable]
public class ServerPlayer
{
    public double balance;
}

// ============================================================================
// FIXED: Server Response Models - Must match actual server JSON structure
// ============================================================================

[Serializable]
public class ServerSpinResponse
{
    public string id = "ResultData";
    public bool success;
    public List<List<string>> matrix;
    public ServerPlayerBalance player;
    public ServerPayload payload;
    public ServerFeaturesResult features;
}

[Serializable]
public class ServerPlayerBalance
{
    public double? balance; // Nullable because server sends null
}

[Serializable]
public class ServerPayload
{
    public List<List<string>> reels;        // Server sends STRINGS not ints!
    public List<ServerWinLine> winningLines; // Server uses "winningLines"
    public double totalWin;                  // Server uses "totalWin"
    public List<ServerWinLine> lineWins;     // Server uses "lineWins" in Diamond Rose
    public double winAmount;                 // Server uses "winAmount" in Diamond Rose
    public int scatterCount;
    public bool scatterTriggered;
    public ServerFreeSpinState freeSpinState; // Can be null
    public bool isRoundOver;                 // True when free spin round is over
    public double totalRoundWin;             // Total round win (at payload level when isRoundOver)
}

[Serializable]
public class ServerFreeSpinState
{
    public bool isActive;
    public int spinsRemaining;
    public int spinsUsed;
    public double totalRoundWin;
    public bool isBought;
    public Dictionary<string, int> stickyWilds;
}

[Serializable]
public class ServerWinLine
{
    public int lineIndex;                    // Server uses "lineIndex"
    public List<string> positions;           // Diamond Rose format: ["1,0", "0,1", "1,2"] (col,row strings)
    public string symbolId;                  // Server sends STRING!
    public string symbolName;                // Diamond Rose sends "Any 7" etc.
    public int matchCount;
    public double basePayout;
    public double payout;
    public double winAmount;                 // Diamond Rose sends winAmount per line
    public int wildMultiplier;
    public List<WildDetail> wildDetails;
}

[Serializable]
public class WildDetail
{
    public int col;
    public int row;
    public int multiplier;
}

[Serializable]
public class ServerFeaturesResult
{
    public ServerFreeSpinResult freeSpins;
}

[Serializable]
public class ServerFreeSpinResult
{
    public bool triggered;
    public int spinsAwarded;
    public bool isFreeSpin;
    public bool isRoundOver;
    public int spinsRemaining;
    public int spinsUsed;  // Added: Server sends this in features.freeSpins
    public int stickyWildsCount;
    public ServerOverlayScatter overlayScatter;
}

[Serializable]
public class ServerOverlayScatter
{
    public bool isTriggered;
    public int count;
    public int extraSpins;
    public List<List<int>> positions;
}

// ============================================================================
// Client-Side Spin Request
// ============================================================================

[Serializable]
public class SpinRequest
{
    public string type = "SPIN";
    public SpinPayload payload;
}

[Serializable]
public class SpinPayload
{
    public int betIndex;
    public bool isFreeSpin;
}


#endregion

#region Game Configuration (Client Side Converted)

[Serializable]
public class GameConfig
{
    public int reelCount = 5;
    public int rowCount = 4;
    public int symbolCount = 13;
    public int paylineCount = 40;
    public List<List<int>> paylines;
    public List<double> availableBets;
    public List<SymbolInfo> symbols;

    // Wild configuration
    public int wildSymbolId = 11;      // Base wild (1x)
    public int wild2xSymbolId = 13;     // Wild 2x multiplier
    public int wild3xSymbolId = 14;     // Wild 3x multiplier
    public int wild5xSymbolId = 15;     // Wild 5x multiplier
    public List<int> wildMultipliers = new List<int> { 1, 2, 3, 5 };

    // Scatter configuration
    public int scatterSymbolId = 12;



    public int betMultiplier = 100;
    public int maxWinMultiplier = 10000;
    public int minWinMultiplier = 10;
    public int initialFreeSpins = 8;
    public ExtraSpinsData extraSpinsData;
}

[Serializable]
public class SymbolInfo
{
    public int id;
    public string name;
    public List<double> multipliers;
    public bool isWild;
    public bool isScatter;
    public int wildMultiplier;
    public double payout;
}

#endregion

#region Player & Game State (Client Side)

[Serializable]
public class PlayerData
{
    public double balance;
    public int currentBetIndex;
}

[Serializable]
public class SpinResult
{
    public List<List<int>> resultMatrix;  // Client uses int matrix
    public double winAmount;
    public List<WinLine> winLines;
    public PlayerData playerData;
    public FreeSpinData freeSpinData;
    public ScatterData scatterData;
    public OverlayScatterData overlayScatterData;
    public Dictionary<string, int> stickyWilds;

    // Server-authoritative free spin state
    public int serverSpinsRemaining;
    public int serverSpinsUsed;
    public double serverTotalRoundWin;
    public bool isRoundOver;
}

[Serializable]
public class WinLine
{
    public int lineId;
    public int symbolId;
    public List<int> positions;  // Flat list: [0, 5, 10, 15, 20]
    public double winAmount;
}

[Serializable]
public class FreeSpinData
{
    public bool isTriggered;
    public int spinsAwarded;
    public int remainingSpins;
    public bool isBought;
}

[Serializable]
public class ScatterData
{
    public bool isTriggered;
    public int scatterCount;
    public double winAmount;
}

[Serializable]
public class OverlayScatterData
{
    public bool isTriggered;
    public int count;
    public int extraSpins;
    public List<List<int>> positions;
}

#endregion

#region Platform Communication

[Serializable]
public class AuthData
{
    public string token;
    public string socketURL;
    public string nameSpace;
}

#endregion

#region Enums

public enum GameState
{
    Initializing,
    Idle,
    Spinning,
    Stopping,
    ShowingWin,
    FreeSpinMode
}

public enum SpinSpeed
{
    Normal,
    Turbo,
    QuickSpin
}

#endregion

#region Helper Classes for Conversion

/// <summary>
/// Converts server data to client GameConfig
/// </summary>
public static class InitDataConverter
{
    internal static GameConfig ConvertToGameConfig(InitData serverData)
    {
        int inferredReelCount = 3;
        int inferredRowCount = 3;
        if (serverData.gameData.lines != null && serverData.gameData.lines.Count > 0)
        {
            inferredReelCount = serverData.gameData.lines[0].Count;
            int maxRow = 0;
            foreach (var line in serverData.gameData.lines)
            {
                foreach (var rowIdx in line)
                {
                    if (rowIdx > maxRow) maxRow = rowIdx;
                }
            }
            inferredRowCount = maxRow + 1;
        }

        var config = new GameConfig
        {
            reelCount = inferredReelCount,
            rowCount = inferredRowCount,
            symbolCount = serverData.uiData.paylines.symbols.Count,
            paylineCount = (serverData.gameData.lines != null) ? serverData.gameData.lines.Count : serverData.gameData.totalLines,
            paylines = serverData.gameData.lines,
            availableBets = serverData.gameData.bets,
            symbols = new List<SymbolInfo>()
        };

        foreach (var serverSymbol in serverData.uiData.paylines.symbols)
        {
            var symbolInfo = new SymbolInfo
            {
                id = serverSymbol.id,
                name = serverSymbol.name,
                multipliers = serverSymbol.multiplier ?? new List<double>(),
                isWild = serverSymbol.name.ToLower().Contains("wild"),
                isScatter = serverSymbol.name.ToLower().Contains("scatter"),
                wildMultiplier = 1,
                payout = serverSymbol.payout
            };

            config.symbols.Add(symbolInfo);

            if (symbolInfo.isWild)
            {
                config.wildSymbolId = symbolInfo.id;
            }
            if (symbolInfo.isScatter)
            {
                config.scatterSymbolId = symbolInfo.id;
            }
        }

        config.betMultiplier = config.paylineCount;

        if (serverData.features != null)
        {
            if (serverData.features.betMultiplier > 0)
            {
                config.betMultiplier = serverData.features.betMultiplier;
            }
            config.maxWinMultiplier = serverData.features.maxWinMultiplier;
            config.minWinMultiplier = serverData.features.minWinMultiplier;
        }

        return config;
    }

    internal static PlayerData ConvertToPlayerData(ServerPlayer serverPlayer, int defaultBetIndex = 0)
    {
        return new PlayerData
        {
            balance = serverPlayer.balance,
            currentBetIndex = defaultBetIndex
        };
    }

    /// <summary>
    /// CRITICAL: Converts server response to client SpinResult
    /// Handles string-to-int conversion, matrix transposition, and wild multiplier mapping
    /// Server sends [row][col] (4 rows x 5 cols), Client needs [col][row] (5 cols x 4 rows)
    /// </summary>
    internal static SpinResult ConvertServerResponseToSpinResult(ServerSpinResponse serverResponse, double currentBalance, double betAmount, GameConfig gameConfig)
    {
        double totalWinAmount = 0;
        if (serverResponse.payload != null)
        {
            totalWinAmount = serverResponse.payload.winAmount > 0 ? serverResponse.payload.winAmount : serverResponse.payload.totalWin;
        }

        double newBalance = serverResponse.player?.balance ?? CalculateNewBalance(currentBalance, betAmount, totalWinAmount);

        // Get server free spin state values
        int spinsRemaining = serverResponse.features?.freeSpins?.spinsRemaining ?? serverResponse.payload?.freeSpinState?.spinsRemaining ?? 0;
        int spinsUsed = serverResponse.features?.freeSpins?.spinsUsed ?? serverResponse.payload?.freeSpinState?.spinsUsed ?? 0;
        double totalRoundWin = (serverResponse.payload != null && serverResponse.payload.totalRoundWin > 0)
            ? serverResponse.payload.totalRoundWin
            : (serverResponse.payload?.freeSpinState?.totalRoundWin ?? 0);
        bool isRoundOver = serverResponse.features?.freeSpins?.isRoundOver ?? (serverResponse.payload != null && serverResponse.payload.isRoundOver);

        var stickyWilds = serverResponse.payload?.freeSpinState?.stickyWilds;

        var reelsSource = serverResponse.matrix ?? serverResponse.payload?.reels;
        var winsSource = serverResponse.payload?.lineWins ?? serverResponse.payload?.winningLines;

        var result = new SpinResult
        {
            // Convert and transpose reels from server format to client format
            resultMatrix = ConvertReelsToMatrix(reelsSource, winsSource, stickyWilds, gameConfig),

            // Map totalWin to winAmount
            winAmount = totalWinAmount,

            // Convert winningLines to winLines
            winLines = ConvertWinningLines(winsSource, gameConfig),

            // Update player data — use server balance directly
            playerData = new PlayerData
            {
                balance = newBalance,
                currentBetIndex = 0 // Will be set by GameManager
            },

            // Convert free spin data
            freeSpinData = serverResponse.features?.freeSpins != null && serverResponse.features.freeSpins.triggered
                ? new FreeSpinData
                {
                    isTriggered = true,
                    spinsAwarded = serverResponse.features.freeSpins.spinsAwarded,
                    remainingSpins = 0,
                    isBought = serverResponse.payload?.freeSpinState?.isBought ?? false
                }
                : null,

            // Convert scatter data
            scatterData = (serverResponse.payload != null && serverResponse.payload.scatterTriggered)
                ? new ScatterData
                {
                    isTriggered = true,
                    scatterCount = serverResponse.payload.scatterCount,
                    winAmount = 0 // Calculate if needed
                }
                : null,

            overlayScatterData = serverResponse.features?.freeSpins?.overlayScatter != null && serverResponse.features.freeSpins.overlayScatter.isTriggered
                ? new OverlayScatterData
                {
                    isTriggered = true,
                    count = serverResponse.features.freeSpins.overlayScatter.count,
                    extraSpins = serverResponse.features.freeSpins.overlayScatter.extraSpins,
                    positions = serverResponse.features.freeSpins.overlayScatter.positions
                }
                : null,

            stickyWilds = serverResponse.payload?.freeSpinState?.stickyWilds,

            // Server-authoritative free spin state
            serverSpinsRemaining = spinsRemaining,
            serverSpinsUsed = spinsUsed,
            serverTotalRoundWin = totalRoundWin,
            isRoundOver = isRoundOver
        };

        return result;
    }


    private static List<List<int>> ConvertReelsToMatrix(List<List<string>> serverReels, List<ServerWinLine> winningLines, Dictionary<string, int> stickyWilds, GameConfig gameConfig)
    {
        int rows = gameConfig != null ? gameConfig.rowCount : 3;
        int cols = gameConfig != null ? gameConfig.reelCount : 3;

        if (serverReels == null || serverReels.Count != rows)
        {
            UnityEngine.Debug.LogError($"Invalid server reels: expected {rows} rows, got {serverReels?.Count}");
            return GenerateDefaultMatrix(rows, cols);
        }

        // Build wild multiplier lookup: [col][row] -> multiplier
        var wildMultipliers = new Dictionary<string, int>();

        // 1. Add winning line wild details (format explicit col, row)
        if (winningLines != null)
        {
            foreach (var line in winningLines)
            {
                if (line.wildDetails != null)
                {
                    foreach (var wild in line.wildDetails)
                    {
                        string key = $"{wild.col}_{wild.row}";
                        wildMultipliers[key] = wild.multiplier;
                    }
                }
            }
        }

        // 2. Add sticky wilds (format row_col) - these override winningLines if they overlap
        // to ensure the authoritative sticky multiplier is used (e.g. 3x instead of 1x)
        if (stickyWilds != null)
        {
            foreach (var kvp in stickyWilds)
            {
                string[] parts = kvp.Key.Split('_');
                if (parts.Length == 2 &&
                    int.TryParse(parts[0], out int row) &&
                    int.TryParse(parts[1], out int col))
                {
                    // Convert row_col to col_row for lookup
                    string key = $"{col}_{row}";
                    wildMultipliers[key] = kvp.Value;
                }
            }
        }

        var matrix = new List<List<int>>();

        // Transpose: iterate by columns
        for (int col = 0; col < cols; col++)
        {
            var column = new List<int>();

            // Each column has rows
            for (int row = 0; row < rows; row++)
            {
                if (col >= serverReels[row].Count)
                {
                    UnityEngine.Debug.LogError($"Invalid server data at row {row}, col {col}");
                    column.Add(0);
                    continue;
                }

                string symbolStr = serverReels[row][col];

                if (!int.TryParse(symbolStr, out int symbolId))
                {
                    UnityEngine.Debug.LogError($"Failed to parse symbol: {symbolStr}");
                    column.Add(0);
                    continue;
                }

                // Check if this is a wild with multiplier
                if (symbolId == gameConfig.wildSymbolId)
                {
                    string key = $"{col}_{row}";
                    if (wildMultipliers.TryGetValue(key, out int multiplier))
                    {
                        // Map wild multiplier to correct symbol ID
                        symbolId = GetWildSymbolIdForMultiplier(multiplier, gameConfig);
                    }
                }

                column.Add(symbolId);
            }

            matrix.Add(column);
        }

        return matrix;
    }

    /// <summary>
    /// Maps wild multiplier to correct symbol ID
    /// 1x → 11 (Wild), 2x → 13 (Wild2x), 3x → 14 (Wild3x), 5x → 15 (Wild5x)
    /// </summary>
    private static int GetWildSymbolIdForMultiplier(int multiplier, GameConfig gameConfig)
    {
        return multiplier switch
        {
            1 => 11,  // Wild (normal)
            2 => 13,  // Wild 2x
            3 => 14,  // Wild 3x
            5 => 15,  // Wild 5x
            _ => 11   // Default to normal wild
        };
    }


    private static List<List<int>> GenerateDefaultMatrix(int rows, int cols)
    {
        var matrix = new List<List<int>>();
        for (int col = 0; col < cols; col++)
        {
            var column = new List<int>();
            for (int row = 0; row < rows; row++)
            {
                column.Add(0);
            }
            matrix.Add(column);
        }
        return matrix;
    }

    /// <summary>
    /// Converts server winningLines to client winLines.
    /// Diamond Rose format: positions are strings like "1,0" (col,row).
    /// Encodes as flat index = row * cols + col.
    /// </summary>
    private static List<WinLine> ConvertWinningLines(List<ServerWinLine> serverWinLines, GameConfig gameConfig)
    {
        var winLines = new List<WinLine>();

        if (serverWinLines == null) return winLines;

        int cols = gameConfig != null ? gameConfig.reelCount : 3;

        foreach (var serverLine in serverWinLines)
        {
            // Try parsing symbolId as int first, fall back to resolving symbolName
            int symbolId = 0;
            if (!string.IsNullOrEmpty(serverLine.symbolId) && int.TryParse(serverLine.symbolId, out int parsedId))
            {
                symbolId = parsedId;
            }
            else if (!string.IsNullOrEmpty(serverLine.symbolName) && gameConfig?.symbols != null)
            {
                // Resolve symbolName (e.g. "Any 7") to an id by matching name
                var match = gameConfig.symbols.Find(s => string.Equals(s.name, serverLine.symbolName, StringComparison.OrdinalIgnoreCase));
                if (match != null)
                {
                    symbolId = match.id;
                }
                else
                {
                    UnityEngine.Debug.LogWarning($"[ConvertWinningLines] Unknown symbolName: {serverLine.symbolName}");
                }
            }

            var flatPositions = new List<int>();

            if (serverLine.positions != null && serverLine.positions.Count > 0)
            {
                // Diamond Rose format: positions are "col,row" strings
                foreach (var posStr in serverLine.positions)
                {
                    string[] parts = posStr.Split(',');
                    if (parts.Length >= 2 &&
                        int.TryParse(parts[0], out int col) &&
                        int.TryParse(parts[1], out int row))
                    {
                        int flatIndex = row * cols + col;
                        flatPositions.Add(flatIndex);
                    }
                    else
                    {
                        UnityEngine.Debug.LogWarning($"[ConvertWinningLines] Failed to parse position string: {posStr}");
                    }
                }
            }
            else
            {
                // Fallback: derive from payline definition + matchCount if positions missing
                UnityEngine.Debug.LogWarning($"[ConvertWinningLines] No positions from server for lineIndex {serverLine.lineIndex}, falling back to payline table");
                if (gameConfig?.paylines != null &&
                    serverLine.lineIndex >= 0 &&
                    serverLine.lineIndex < gameConfig.paylines.Count)
                {
                    var payline = gameConfig.paylines[serverLine.lineIndex];
                    for (int col = 0; col < serverLine.matchCount && col < payline.Count; col++)
                    {
                        int row = payline[col];
                        flatPositions.Add(row * cols + col);
                    }
                }
            }

            // Use winAmount if available, fallback to payout
            double lineWin = serverLine.winAmount > 0 ? serverLine.winAmount : serverLine.payout;

            winLines.Add(new WinLine
            {
                lineId = serverLine.lineIndex,
                symbolId = symbolId,
                positions = flatPositions,
                winAmount = lineWin
            });
        }

        return winLines;
    }

    private static double CalculateNewBalance(double currentBalance, double betAmount, double winAmount)
    {
        return currentBalance - betAmount + winAmount;
    }
}

#endregion