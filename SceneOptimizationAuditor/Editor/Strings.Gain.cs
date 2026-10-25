namespace UnityTools.SceneOptimization
{
    public static partial class Strings
    {
        static void AddGain()
        {
            // Gains of fixes: a row, a group, the total and the order of the list.
            Add("ui.gain.mb", "−{0} MB", "−{0} МБ");
            Add("ui.gain.dc", "−{0} DC", "−{0} DC");
            Add("ui.gain.tris", "−{0} tris", "−{0} tris");
            Add("ui.gain.mb.long", "−{0} MB", "−{0} МБ");
            Add("ui.gain.dc.long", "−{0} draw calls", "−{0} draw calls");
            Add("ui.gain.tris.long", "−{0} triangles", "−{0} треугольников");
            Add("ui.gain.tip", "Estimated: what the fix frees — memory, draw calls or triangles. ≈ marks a rough estimate.",
                "Оценка: что освободит исправление — память, draw calls или треугольники. «≈» — грубая оценка.");
            Add("ui.gain.group.tip", "What the fixes of the shown rows free together.", "Что освободят исправления показанных записей вместе.");
            Add("ui.gain.sort", "By gain", "По выигрышу");
            Add("ui.gain.sort.tip",
                "Groups by the share of the budgets their fixes free: MB of the texture memory threshold, draw calls and triangles of theirs. Groups without an estimate follow in the usual order.",
                "Группы по доле бюджета, которую освобождают их исправления: МБ — от порога памяти текстур, draw calls и треугольники — от своих порогов. Группы без оценки — после, как обычно.");
            Add("ui.gain.total", "Fixing everything:", "Если исправить всё:");
            Add("ui.gain.total.tip", "Every problem that is not acceptable.", "Все проблемы, кроме допустимых.");
            Add("ui.gain.total.note", "Acceptable rows left out; of several rows of one asset or object the largest estimate counts.",
                "Без допустимых; у нескольких записей одного ассета или объекта считается наибольшая оценка.");
        }
    }
}
