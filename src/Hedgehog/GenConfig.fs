namespace Hedgehog

type GenConfig<'a> = private {
    Formatter : 'a -> string
}

module GenConfig =

    let defaultConfig<'a> =
        let formatter: 'a -> string = sprintf "%A"
        { Formatter = formatter }

    let getFormatter (config : GenConfig<'a>) : ('a -> string) =
        config.Formatter

    let setFormatter (formatter : 'a -> string) (config : GenConfig<'a>) : GenConfig<'a> =
        { config with Formatter = formatter }
