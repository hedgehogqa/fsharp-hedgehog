namespace Hedgehog

type GenConfig<'a> = private {
    Formatter : 'a -> string
}

module GenConfig =

    let defaultConfig () =
        { Formatter = sprintf "%A" }

    let getFormatter (config : GenConfig<'a>) : ('a -> string) =
        config.Formatter

    let setFormatter (formatter : 'a -> string) (config : GenConfig<'a>) : GenConfig<'a> =
        { config with Formatter = formatter }
