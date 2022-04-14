open canopy.classic
open System
open WordleBot
    
[<EntryPoint>]
let main argv =

    // Play the game until it ends.
    let rec takeTurn (wordlist, guess) =
        if Board.isComplete () then
            wordlist, guess
        else
            Board.enterGuess guess
            let response = Board.getLatestResponse ()            
            takeTurn (Wordlist.filterAndGuess wordlist response)

    // Get the word list.
    let fullWordlist = Wordlist.getAll ()
    
    // Start the Wordle game.
    Board.startGame ()

    // Get starting word.
    let startingWord = Wordlist.getStartingWord ()

    // Play the game.
    takeTurn (fullWordlist, startingWord) |> ignore

    // Update stats.
    Board.getBoard () |> StatsCreator.update

    printfn "Press [Enter] to exit."
    Console.ReadKey() |> ignore
    quit()
    0