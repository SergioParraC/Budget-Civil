using BudgetCivil.Domain.Wbs;
using FluentAssertions;

namespace BudgetCivil.Tests.Unit.Domain;

/// <summary>
/// `T-U-18` y `T-U-19`: código manual, formato, inmutabilidad, cantidad, movimiento y orden de borrado
/// (`SC-EDT-06`, `SC-EDT-08`…`29`, `SC-EDT-51`…`75`).
/// </summary>
public sealed class WbsDomainTests
{
    private static readonly Guid ProjectId = Guid.NewGuid();
    private static readonly Guid ActorId = Guid.NewGuid();
    private static readonly DateTime UtcNow = new(2026, 11, 14, 12, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData("CIM.1", true)]
    [InlineData("a", true)]
    [InlineData("A-1_2/3*4=", true)]
    [InlineData("19,77", true)]
    [InlineData("(A)%&#'¡?¿", true)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("ABCDEFGHIJK", false)]
    [InlineData("CIM@1", false)]
    [InlineData("CIMÁ", false)]
    public void La_politica_valida_formato_longitud_y_conjunto(string code, bool expected)
    {
        WbsCodePolicy.IsValidCode(code).Should().Be(expected);
    }

    [Fact]
    public void Normalizar_conserva_mayusculas_y_minusculas_y_recorta()
    {
        // SC-EDT-09 y SC-EDT-24: se acepta `cim` y `CIM`; se conserva lo escrito.
        WbsCodePolicy.NormalizeCode("  cIm.1  ").Should().Be("cIm.1");
        WbsCodePolicy.NormalizeCode("CIM").Should().Be("CIM");
    }

    [Fact]
    public void Un_codigo_invalido_no_se_normaliza()
    {
        var action = () => WbsCodePolicy.NormalizeCode("CIM@1");

        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void El_capitulo_se_crea_con_codigo_manual_autor_y_fecha()
    {
        // SC-EDT-01 y SC-EDT-40.
        var chapter = CreateChapter("EST", null, 0);

        chapter.Code.Code.Should().Be("EST");
        chapter.ProjectId.Should().Be(ProjectId);
        chapter.ParentChapterId.Should().BeNull();
        chapter.LastModifiedByUserId.Should().Be(ActorId);
        chapter.LastModifiedAtUtc.Should().Be(UtcNow);
        chapter.Code.NodeType.Should().Be(WbsNodeType.Chapter);
    }

    [Fact]
    public void El_nombre_vacio_es_invalido()
    {
        var action = () => WbsChapter.Create(
            ProjectId,
            null,
            WbsCode.Create(ProjectId, "EST", WbsNodeType.Chapter),
            "   ",
            0,
            ActorId,
            UtcNow);

        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void El_item_raiz_no_tiene_capitulo_y_el_apu_se_ignora()
    {
        // SC-EDT-06, SC-EDT-28 y SC-EDT-29.
        var item = CreateItem(null, "CIM.1", 12.5m, 0);

        item.WbsChapterId.Should().BeNull();
        item.ApuId.Should().BeNull();
        item.QuantityWork.Should().Be(12.5m);
        item.Code.NodeType.Should().Be(WbsNodeType.Item);
    }

    [Fact]
    public void La_cantidad_de_obra_cero_es_valida()
    {
        var item = CreateItem(null, "CIM.1", 0m, 0);

        item.QuantityWork.Should().Be(0m);
    }

    [Fact]
    public void La_cantidad_de_obra_negativa_es_invalida()
    {
        // SC-EDT-27.
        var action = () => CreateItem(null, "CIM.1", -1m, 0);

        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Cambiar_el_codigo_de_un_capitulo_no_reescribe_a_los_hijos()
    {
        // SC-EDT-15 y SC-EDT-19.
        var chapter = CreateChapter("1", null, 0);
        var child = CreateChapter("1.1", chapter.Id, 0, "Subcapítulo");

        chapter.ChangeCode(WbsCode.Create(ProjectId, "A", WbsNodeType.Chapter));

        chapter.Code.Code.Should().Be("A");
        child.Code.Code.Should().Be("1.1");
    }

    [Fact]
    public void Mover_o_reordenar_no_cambia_el_codigo()
    {
        // SC-EDT-17 y SC-EDT-18.
        var chapter = CreateChapter("1.2", null, 0);
        var code = chapter.Code.Code;

        chapter.MoveTo(null, 4);
        chapter.SetOrderIndex(2);

        chapter.Code.Code.Should().Be(code);
        chapter.OrderIndex.Should().Be(2);
    }

    [Fact]
    public void La_autoría_y_las_propiedades_criticas_no_tienen_setter_publico()
    {
        // D-116 y D-118: el cliente no fija autoría, proyecto, código, posición ni versión.
        typeof(WbsChapter).GetProperty(nameof(WbsChapter.ProjectId))!.SetMethod!.IsPublic.Should().BeFalse();
        typeof(WbsChapter).GetProperty(nameof(WbsChapter.CodeId))!.SetMethod!.IsPublic.Should().BeFalse();
        typeof(WbsChapter).GetProperty(nameof(WbsChapter.OrderIndex))!.SetMethod!.IsPublic.Should().BeFalse();
        typeof(WbsChapter).GetProperty(nameof(WbsChapter.LastModifiedByUserId))!.SetMethod!.IsPublic.Should().BeFalse();
        typeof(WbsChapter).GetProperty(nameof(WbsChapter.RowVersion))!.SetMethod!.IsPublic.Should().BeFalse();
        typeof(WbsItem).GetProperty(nameof(WbsItem.WbsChapterId))!.SetMethod!.IsPublic.Should().BeFalse();
        typeof(WbsItem).GetProperty(nameof(WbsItem.ApuId))!.SetMethod!.IsPublic.Should().BeFalse();
    }

    [Fact]
    public void El_destino_dentro_del_propio_subarbol_es_un_ciclo()
    {
        // SC-EDT-67.
        var chapter = CreateChapter("1", null, 0);
        var child = CreateChapter("1.1", chapter.Id, 0, "Subcapítulo");
        var parents = new Dictionary<Guid, Guid?>
        {
            [chapter.Id] = null,
            [child.Id] = chapter.Id
        };

        var validation = WbsTreePolicy.ValidateMove(
            WbsNodeType.Chapter,
            chapter.Id,
            child.Id,
            targetExists: true,
            targetIsChapter: true,
            targetSameProject: true,
            parents);

        validation.IsValid.Should().BeFalse();
        validation.ErrorCode.Should().Be(WbsTreePolicy.MoveCycle);
    }

    [Fact]
    public void El_destino_que_no_es_capitulo_es_error_de_validacion()
    {
        // SC-EDT-69.
        var validation = WbsTreePolicy.ValidateMove(
            WbsNodeType.Item,
            Guid.NewGuid(),
            Guid.NewGuid(),
            targetExists: true,
            targetIsChapter: false,
            targetSameProject: true,
            new Dictionary<Guid, Guid?>());

        validation.IsValid.Should().BeFalse();
        validation.ErrorCode.Should().Be(WbsTreePolicy.ValidationError);
    }

    [Fact]
    public void El_destino_inexistente_o_ajeno_es_no_encontrado()
    {
        // SC-EDT-68.
        var validation = WbsTreePolicy.ValidateMove(
            WbsNodeType.Item,
            Guid.NewGuid(),
            Guid.NewGuid(),
            targetExists: false,
            targetIsChapter: false,
            targetSameProject: false,
            new Dictionary<Guid, Guid?>());

        validation.IsValid.Should().BeFalse();
        validation.ErrorCode.Should().Be(WbsTreePolicy.TargetNotFound);
    }

    [Theory]
    [InlineData(null, 3, 3)]
    [InlineData(0, 3, 0)]
    [InlineData(3, 3, 3)]
    public void El_indice_destino_se_interpreta_tras_retirar_el_nodo(int? targetIndex, int remaining, int expected)
    {
        // SC-EDT-70 y SC-EDT-75.
        WbsTreePolicy.ResolveTargetIndex(targetIndex, remaining).Should().Be(expected);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(4)]
    public void Un_indice_destino_fuera_de_rango_es_invalido(int targetIndex)
    {
        var action = () => WbsTreePolicy.ResolveTargetIndex(targetIndex, 3);

        action.Should().Throw<InvalidOperationException>()
            .WithMessage(WbsTreePolicy.ValidationError);
    }

    [Fact]
    public void El_borrado_ordena_los_capitulos_de_hijos_a_padres()
    {
        // SC-EDT-54 y D-119.
        var root = CreateChapter("1", null, 0);
        var child = CreateChapter("1.1", root.Id, 0, "Subcapítulo");
        var grandchild = CreateChapter("1.1.1", child.Id, 0, "Nieto");

        var ordered = WbsTreePolicy.OrderChaptersForDeletion([root, child, grandchild]);

        ordered.Select(chapter => chapter.Id).Should().ContainInOrder(grandchild.Id, child.Id, root.Id);
    }

    private static WbsChapter CreateChapter(
        string code,
        Guid? parentChapterId,
        int orderIndex,
        string name = "Capítulo")
    {
        return WbsChapter.Create(
            ProjectId,
            parentChapterId,
            WbsCode.Create(ProjectId, code, WbsNodeType.Chapter),
            name,
            orderIndex,
            ActorId,
            UtcNow);
    }

    private static WbsItem CreateItem(Guid? chapterId, string code, decimal quantity, int orderIndex)
    {
        return WbsItem.Create(
            ProjectId,
            chapterId,
            WbsCode.Create(ProjectId, code, WbsNodeType.Item),
            quantity,
            orderIndex,
            ActorId,
            UtcNow);
    }
}
