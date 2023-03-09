using System;
using Microsoft.Extensions.Logging;

namespace kiota;

internal static class ILoggerExtensions
{
    /// <summary>
    /// Appends internal logging information to the logger
    /// </summary>
    /// <param name="logger">The logger to append to</param>
    public static void AppendInternalTracing(this ILogger logger)
    {
        // This is a special message for Kiota's birthday
        // If you're reading this, move along, nothing to see here and keep quiet about it
        // Otherwise, you'll be cursed with a lifetime of bad luck
        if (logger == null) return;
        var currentDateTime = DateTime.UtcNow;
        if (currentDateTime.Month != 3 || currentDateTime.Day != 9 || currentDateTime.Year < 2024) return;
        logger.LogWarning(
@"                  ▒▒░░▒▒        ░░        ▒▒░░▒▒                  
                    ▒▒        ██████        ▒▒                    
                  ██████      ██▒▒██      ██████                  
                  ██▒▒██      ██▒▒██      ██▒▒██                  
                  ██▒▒██      ██▒▒██      ██▒▒██                  
                  ██▒▒██      ██▒▒██      ██▒▒██                  
                  ██▒▒████▒▒▒▒██▒▒████▒▒▒▒██▒▒██                  
              ▓▓▓▓██▒▒██▓▓░░▒▒██▒▒██▓▓░░▒▒██▒▒██▓▓▓▓              
          ████▒▒  ██▒▒██  ▒▒  ██▒▒██  ▒▒  ██▒▒██  ▒▒████          
        ██  ▒▒  ▒▒██▒▒██▒▒  ▒▒██▒▒██▒▒  ▒▒██▒▒██▒▒  ▒▒  ██        
        ██░░░░░░░░██▒▒██░░░░░░▓▓████░░░░░░▓▓████░░░░░░░░██        
        ██▒▒▒▒  ▒▒  ████▒▒  ▒▒  ▒▒  ▒▒  ▒▒  ▒▒  ▒▒  ▒▒▒▒██        
        ██▒▒▒▒▒▒  ▒▒  ▒▒  ▒▒  ▒▒  ▒▒  ▒▒  ▒▒  ▒▒  ▒▒▒▒▒▒██        
        ██    ▒▒▒▒▒▒▒▒  ▒▒  ▒▒  ▒▒  ▒▒  ▒▒  ▒▒▒▒▒▒▒▒    ██        
        ██    ░░░░░░▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒░░░░░░    ██        
        ██                                              ██        
        ██    ░░              ░░              ░░        ██        
      ██        ▒▒              ▒▒              ▒▒        ██      
      ██▒▒▒▒          ▒▒▒▒▒▒          ▒▒▒▒▒▒          ▒▒▒▒██      
      ██░░░░▒▒      ▒▒░░░░░░▒▒      ▒▒░░░░░░▒▒      ▒▒░░░░██      
      ██▒▒░░░░▒▒▒▒▒▒░░░░░░░░░░▒▒▒▒▒▒░░░░░░░░░░▒▒▒▒▒▒░░▒▒░░██      
      ██░░▒▒░░░░░░░░░░░░▒▒░░░░░░░░░░░░░░▒▒░░░░░░░░░░░░░░▒▒██      
    ██▒▒██░░░░░░░░░░░░░░░░▒▒░░░░░░░░░░░░░░▒▒░░░░░░░░░░░░██▒▒██    
  ██░░▒▒▒▒████░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░████▒▒▒▒░░██  
  ██░░░░▒▒▒▒▒▒██████░░░░░░░░░░░░░░░░░░░░░░░░░░██████▒▒▒▒▒▒░░░░██  
    ██░░▒▒▒▒▒▒▒▒▒▒▒▒██████████████████████████▒▒▒▒▒▒▒▒▒▒▒▒▒▒██    
      ████░░░░░░░░░░▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒░░░░░░░░░░████      
          ████████░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░████████          
                  ██████████████████████████████                  
 ________________________________________________________________ 
 |                                                              | 
 |                                                              | 
 |                    HAPPY BIRTHDAY KIOTA!                     | 
 |                                                              | 
 |--------------------------------------------------------------| 
 |                                                              | 
 |          MADE WITH LOVE BY THE MICROSOFT GRAPH TEAM          | 
 |                  FROM CANADA, KENYA, UK & US                 | 
 |                          2023-03-09                          | 
 |                                                              | 
 |--------------------------------------------------------------| 
 |                                                              | 
 |                        NOW THE CREDITS                       | 
 |                                                              | 
 |--------------------------------------------------------------| 
 |                                                              | 
 |                          CO-FOUNDERS                         | 
 |                                                              | 
 |                         Darrel Miller                        | 
 |                         Vincent Biret                        | 
 |                                                              | 
 |                      PROGRAM MANAGEMENT                      | 
 |                                                              | 
 |                         Rabeb Othmani                        | 
 |                        Sébastien Levert                      | 
 |                                                              | 
 |                PROGRAM MANAGEMENT (LANGUAGES)                | 
 |                                                              | 
 |                        Carol Kigoonya                        | 
 |                        Isaac Vargas                          | 
 |                        Maisa Rissi                           | 
 |                                                              | 
 |                         ENGINEERING                          | 
 |                                                              | 
 |                        Andrew Omondi                         | 
 |                        DeVere Dyett                          | 
 |                        Caleb Kiage                           | 
 |                        Irvine Sunday                         | 
 |                        Japheth Obala                         | 
 |                        Nikitha Chettiar                      | 
 |                        Philip Gichuhi                        | 
 |                        Ramses Sanchez                        | 
 |                        Ronald Kudoyi                         | 
 |                        Samwel Kanda                          | 
 |                        Shem Ogumbe                           | 
 |                        Silas Keneth                          | 
 |                                                              | 
 |                        DOCUMENTATION                         | 
 |                                                              | 
 |                        Jason Johnston                        | 
 |                                                              | 
 |                          ADVOCACY                            | 
 |                                                              | 
 |                       Waldek Mastykarz                       | 
 |                                                              | 
 |--------------------------------------------------------------| ");
    }
}
