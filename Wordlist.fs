namespace WordleBot

open System
open System.Net
open System.IO

module Wordlist =

    // Needed for random word picks.
    let randomSeed = Random()

    // Add to list of words to start with (these are potentially the top 10?)
    let startingWordCandidates =
        [|
            "chare"
            "crate"
            "peart"
            "speat"
            "reast"
            "trine"
            "tread"
            "blate"
            "reist"
            "alist"
        |]

    /// Get official word list.
    let getAll () =

        let url = "https://www.nytimes.com/games/wordle/main.4d41d2be.js"        

        let req = WebRequest.Create(Uri(url))
        use resp = req.GetResponse()
        use stream = resp.GetResponseStream()
        use reader = new StreamReader(stream)

        let html = reader.ReadToEnd()
        let startIndex, endIndex =        
            html.IndexOf("Ma=["), html.IndexOf("],Oa=")
        
        let rawList = html.[(startIndex+4)..(endIndex-1)].Replace("\"","")
        rawList.Split(',')
        |> Array.sort

    /// Removes the letters in word2 from word1 preserving duplicates.
    let private diffWord word1 word2 =

        let word1', word2' =
            word1 |> Array.ofSeq,
            word2 |> Array.ofSeq

        word2'
        |> Array.iter (fun l2 ->
            if word1' |> Array.contains l2 then
                let idx = word1' |> Array.findIndex (fun l1 -> l1 = l2)
                word1'.[idx] <- ' ')

        word1'
        |> Array.filter (fun l -> l <> ' ')
        |> String.Concat
        
    /// Given the current word list and the board, return new filtered wordlist and
    /// guess for the next word.
    let filterAndGuess (currentWordList: string array) (latestResponse: Response) =

        // Firstly, collate all hints. 
        let letterHints =
            Seq.zip latestResponse.Word latestResponse.Hints

        let getHintLetters (hintType: Hint) =
            letterHints
            |> Seq.mapi (fun i (l,h) -> match h with | h when h = hintType -> i, l | _ -> -1, l)
            |> Seq.filter (fun (i,_) -> i >= 0)                

        let correctLetters = getHintLetters Correct
        let correctIndexes = correctLetters |> Seq.map (fst)

        let presentLetters = getHintLetters Present
        let absentLetters  = getHintLetters Absent |> Seq.map (snd)

        // Filter down to all words containing correct letters.
        let filteredWordList =
            currentWordList
            |> Array.filter (fun w -> correctLetters |> Seq.forall (fun (i,l) -> w.[i] = l))

        // Filter down to all words containing present letters.
        let filteredWordList' =
            filteredWordList
            |> Array.filter (fun w ->                
                presentLetters |> Seq.forall (fun (i,l) -> w.Contains(l) && w.[i] <> l && not (correctIndexes |> Seq.contains i))) 

        // Filter down to all words not containing absent letters.
        let presentLetters =
            let cl = correctLetters |> Seq.map (snd)
            let pl = presentLetters |> Seq.map (snd)
            (Seq.append cl pl)
            |> String.Concat

        let filteredWordList'' =
            filteredWordList'
            |> Array.filter (fun w ->
                let w' = diffWord w presentLetters
                absentLetters |> Seq.forall (fun al -> not(w'.Contains(al))))

        // We have now filtered down as far as we can go, return filtered list and
        // a random word from it.
        let randomIndex = randomSeed.Next(0, filteredWordList''.Length)
        filteredWordList'', filteredWordList''.[randomIndex]
        
    /// Get starting word to kick off Wordle game.
    let getStartingWord () =
        let randomIndex = randomSeed.Next(0,startingWordCandidates.Length)
        startingWordCandidates.[randomIndex]