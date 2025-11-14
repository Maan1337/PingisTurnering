using NUnit.Framework;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using PingisTurnering;

namespace PingisTurnering.Tests;

[TestFixture]
[Apartment(ApartmentState.STA)] // WinForms requires STA threads
public class PlayerSetupFormTests
{
    [Test]
    public void Constructor_WithDefaultNames_PopulatesSequentialLines()
    {
        const int count = 5;
        using var form = new PlayerSetupForm(count);

        // Access private TextBox
        var txtNames = ReflectionHelpers.GetPrivateField<TextBox>(form, "_txtNames");
        var nud = ReflectionHelpers.GetPrivateField<NumericUpDown>(form, "_nudPlayers");

        Assert.That(nud.Value, Is.EqualTo(count), "NumericUpDown initial value should match requested count.");
        Assert.That(txtNames.Lines.Length, Is.EqualTo(count), "Default number of lines should equal requested count.");
        Assert.That(txtNames.Lines, Is.EqualTo(Enumerable.Range(1, count).Select(i => i.ToString()).ToArray()));
        Assert.That(form.NumberOfPlayers, Is.EqualTo(0), "NumberOfPlayers should not be set before successful OK.");
        Assert.That(form.PlayerNames, Is.Empty, "PlayerNames should be empty before successful OK.");
    }

    [Test]
    public void Constructor_WithInitialNames_UsesProvidedNames()
    {
        var names = new[] { "Alice", "Bob" };
        using var form = new PlayerSetupForm(14, names); // initialNumberOfPlayers ignored for lines when names passed

        var txtNames = ReflectionHelpers.GetPrivateField<TextBox>(form, "_txtNames");
        Assert.That(txtNames.Lines, Is.EqualTo(names), "Provided names should populate text box.");
    }

    [Test]
    public void OkClick_WithMatchingCounts_SetsDialogResultOkAndPopulatesProperties()
    {
        var names = new[] { "  Ann  ", "Bo" };
        using var form = new PlayerSetupForm(2, names);

        var nud = ReflectionHelpers.GetPrivateField<NumericUpDown>(form, "_nudPlayers");
        nud.Value = 2;

        var btnOk = ReflectionHelpers.GetPrivateField<Button>(form, "_btnOk");

        // Ensure controls are created
        form.CreateControl();

        btnOk.PerformClick();

        Assert.That(form.DialogResult, Is.EqualTo(DialogResult.OK), "DialogResult should be OK after successful validation.");
        Assert.That(form.NumberOfPlayers, Is.EqualTo(2));
        Assert.That(form.PlayerNames, Is.EquivalentTo(new[] { "Ann", "Bo" }), "Names should be trimmed.");
    }

    [Test]
    public void OkClick_WithMismatchedCounts_DoesNotCloseOrSetProperties()
    {
        var names = new[] { "P1", "P2", "P3" };
        using var form = new PlayerSetupForm(3, names);

        var nud = ReflectionHelpers.GetPrivateField<NumericUpDown>(form, "_nudPlayers");
        nud.Value = 4; // Force mismatch

        var btnOk = ReflectionHelpers.GetPrivateField<Button>(form, "_btnOk");
        form.CreateControl();

        // Suppress MessageBox UI by installing a dummy message filter (optional).
        // (We simply perform the click; the MessageBox may appear during interactive runs.)
        btnOk.PerformClick();

        Assert.That(form.DialogResult, Is.Not.EqualTo(DialogResult.OK), "DialogResult must not be OK when validation fails.");
        Assert.That(form.NumberOfPlayers, Is.EqualTo(0), "NumberOfPlayers should remain unset.");
        Assert.That(form.PlayerNames, Is.Empty, "PlayerNames should remain empty.");
    }

    [Test]
    public void NumericUpDown_InitialValue_IsClampedWithinRange()
    {
        using var formLow = new PlayerSetupForm(0);
        var nudLow = ReflectionHelpers.GetPrivateField<NumericUpDown>(formLow, "_nudPlayers");
        Assert.That(nudLow.Value, Is.EqualTo(2), "Value below minimum should clamp to 2.");

        using var formHigh = new PlayerSetupForm(999);
        var nudHigh = ReflectionHelpers.GetPrivateField<NumericUpDown>(formHigh, "_nudPlayers");
        Assert.That(nudHigh.Value, Is.EqualTo(256), "Value above maximum should clamp to 256.");
    }
}