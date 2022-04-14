namespace WordleBot

open canopy.classic
open canopy.types
open System

/// Represents a Wordle hint.
type Hint =
    | Absent
    | Present
    | Correct

/// Represents a Wordle response in a row.
type Response =
    {
        Word : string
        Hints: Hint array
    }

/// Represents the Wordle board.
type Board = Response option array

module Board =

    /// Start a Wordle game.
    let startGame () =
    
        // Set up Canopy driver.
        canopy.configuration.chromeDir <- AppContext.BaseDirectory
    
        // Start an instance of Chrome.
        start chrome        
        pin FullScreen

        // Pass through GDPR message and instructions screen.
        url "https://www.nytimes.com/games/wordle/index.html"    
        click "#pz-gdpr-btn-accept"
        click "/html/body"

    /// Enter a word guess.
    let enterGuess word =

        word |> Seq.iter (fun l -> press (l |> string))    
        press enter

        sleep 2 // This is required to give the animation time to finish.

    /// Parse a string containing all hints for a word.
    let private parseHint (hintString: string) =

        match hintString with
        | "" -> None
        | _ ->
            hintString.Split(',')
            |> Array.map (fun hint ->
                match hint with
                | "absent"  -> Absent
                | "correct" -> Correct
                | _         -> Present)
            |> Some
    
    /// Get full board based on responses on last guess.
    let getBoard () =
                
        let words =
            let rowJsWord =
                "return document.querySelector('game-app').shadowRoot"+
                ".querySelector('game-theme-manager').querySelector('#board')"+
                ".querySelectorAll('game-row')[{index}]._letters;"
            [|0..5|]
            |> Array.map (fun i -> js (rowJsWord.Replace("{index}", i |> string)))
            |> Array.map (string)
    
        let hints =
            let rowJsHint =
                "return document.querySelector('game-app').shadowRoot"+
                ".querySelector('game-theme-manager').querySelector('#board')"+
                ".querySelectorAll('game-row')[{index}]._evaluation.toString();"
            [|0..5|]
            |> Array.map (fun i -> js (rowJsHint.Replace("{index}", i |> string)))
            |> Array.map (string)
    
        Array.zip words hints
        |> Array.map (fun (w,h) ->                
            match parseHint h with
            | Some hints -> Some { Word = w; Hints = hints }
            | None -> None)

    /// Return true if game has been won yet.
    let hasWon (board: Board) =

        let foundWord (response: Response option) =
            match response with
            | None -> false
            | Some response ->
                response.Hints
                |> Array.map (fun h -> if h = Correct then true else false)
                |> Array.reduce (&&)
                 
        board
        |> Array.map (foundWord)
        |> Array.reduce (||)

    /// Return true if game has been lost.
    let hasLost (board: Board) =

        let foundAttempt (response: Response option) =
            match response with
            | None -> 0
            | Some _ -> 1

        board
        |> Array.map (foundAttempt)
        |> Array.sum
        |> (=) 6

    /// Returns true if game is complete, false otherwise.
    let isComplete () =
        
        let board = getBoard ()        
        hasWon board || hasLost board

    /// Get the current score.
    let getScore (board: Board) =
        if hasLost board then
            7 // Failure = 7!
        else
            board
            |> Array.findIndexBack (fun r -> match r with | Some _ -> true | None -> false)
            |> (+) 1

    /// Get the last respose entered.
    let getLatestResponse () =

        getBoard ()
        |> Array.takeWhile (fun r -> match r with | Some _ -> true | None -> false)
        |> Array.last
        |> Option.get