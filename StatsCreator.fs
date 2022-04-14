namespace WordleBot

open System
open System.IO

module StatsCreator =

    // Update this path if stats file changes.
    let private statsFilePath = "/Users/Nick/development/workspace/WordleBot/Stats/WordleBotStats.csv"

    /// Get current date.
    let private getDate () =
        DateTime.Now.ToString("dd-MMM-yyyy")

    /// Get guess word (1-6.)
    let private getGuessedWord n (board: Board) =
        match board.[n] with
        | Some response -> response.Word
        | None -> ""

    /// Get rolling average after current run.
    let private getAverage todaysScore =
        let stats = File.ReadLines(statsFilePath) |> Seq.skip 1 // Skip header.
        let count = stats |> Seq.length
        let previousScores = stats |> Seq.map (fun s -> (s.Split ',').[7] |> int)

        let sumScores =
            previousScores
            |> Seq.sum
            |> (+) todaysScore
            |> double

        sumScores / double (count+1)

    /// Update daily WordleBot stats.
    let update (board: Board) =

        let date = getDate ()
        let guesses = Array.init 6 (fun i -> board |> getGuessedWord i)
        let score = board |> Board.getScore
        let average = getAverage score
        printfn "Current average is %.2f." average 

        use sw = File.AppendText(statsFilePath)
        sw.WriteLine(sprintf "%s,%s,%s,%s,%s,%s,%s,%d,%.2f"
            date
            guesses.[0]
            guesses.[1]
            guesses.[2]
            guesses.[3]
            guesses.[4]
            guesses.[5]
            score
            average
        )

        sw.Close ()