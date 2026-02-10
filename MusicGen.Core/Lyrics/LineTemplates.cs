namespace MusicGen.Core.Lyrics;

public static class LineTemplates
{
    public static readonly string[] Templates =
    {
        // Simple
        "{adj} {noun}",
        "{noun} {verb}",
        "the {noun} {verb}",
        "a {adj} {noun}",
        // Descriptive
        "{adj} {noun} {verb}",
        "{noun} in the {noun}",
        "the {adj} {noun}",
        "{verb} the {noun}",
        "like a {adj} {noun}",
        "under the {adj} {noun}",
        "beyond the {noun}",
        // Action
        "I {verb} the {noun}",
        "we {verb} in the {noun}",
        "she {verb} a {adj} {noun}",
        "he {verb} with {noun}",
        "they {verb} like {noun}",
        // Complex
        "the {noun} {verb} in the {adj} {noun}",
        "never {verb} the {adj} {noun}",
        "always {verb} a {noun}",
        "where the {noun} {verb}",
        "when the {noun} {verb} the {noun}",
        "{adj} {noun}, {adj} {noun}",
        "{verb}, {verb}, {verb}",
        "no {noun} left to {verb}",
        "only {noun} remains",
        // Poetic/Abstract
        "{noun} of {noun}",
        "{adj} {noun} of {noun}",
        "{noun} {verb} {noun}",
        "between {noun} and {noun}",
        "through the {adj} {noun}",
        "waiting for the {noun}",
        "lost in the {noun}",
        "{verb} away the {noun}",
        "{noun} calls my name",
    };
}
