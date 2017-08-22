Number.prototype.format = function (n, x) {
    var re = '\\d(?=(\\d{' + (x || 3) + '})+' + (n > 0 ? '\\.' : '$') + ')';
    return this.toFixed(Math.max(0, ~~n)).replace(new RegExp(re, 'g'), '$&,');
};

function calc() {
    var pri = intVal($fAdd.val()) + intVal($fBase.val()),
        payment = pri + intVal($fEscrow.val()) + intVal($fInterest.val()),
        principal = currentPrincipal - pri;
    $rPayment.text('$' + payment.format(2));
    $rPrincipal.text('$' + principal.format(2));
}

$(function () {
    var year, date,
        months = ["January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December"];

    var intVal = function (num) {
        return typeof num === 'string' ?
        num.replace(/[\$,]/g, '') * 1 :
        typeof num === 'number' ?
            num : 0;
    };

    if ($("#loanPaymentId").length) {
        var lpId = $("#loanPaymentId").val(),
            rlpStr = "#LoanPayment-" + lpId,
            currentPrincipal = intVal($(rlpStr).attr("data-current-principal")),

            $fDatePaid = $("#DatePaid"),
            $fInterest = $("#Interest"),
            $fEscrow = $("#Escrow"),
            $fBase = $("#BasicPayment"),
            $fAdd = $("#AddPayment"),

            $rInterest = $(rlpStr + " .interest"),
            $rEscrow = $(rlpStr + " .escrow"),
            $rBase = $(rlpStr + " .base"),
            $rAdd = $(rlpStr + " .add"),
            $rPayment = $(rlpStr + " .payment"),
            $rPrincipal = $(rlpStr + " .principal");

        year = $(rlpStr + " th").attr("data-search");
        $(rlpStr).removeClass("alert-danger").removeClass("alert-success").removeClass("alert-warning").addClass("alert-info");

        $fInterest.keyup(function () { $rInterest.text('$' + intVal($(this).val()).format(2)); calc(); })
            .focus(function () { $(this).select(); });

        $fEscrow.keyup(function () { $rEscrow.text('$' + intVal($(this).val()).format(2)); calc(); })
            .focus(function () { $(this).select(); });

        $fBase.keyup(function () { $rBase.text('$' + intVal($(this).val()).format(2)); calc(); })
            .focus(function () { $(this).select(); });

        $fAdd.keyup(function () { $rAdd.text('$' + intVal($(this).val()).format(2)); calc(); })
            .focus(function () { $(this).select(); });
    }
    else if ($("#billPaymentId").length) {
        var bpId = $("#billPaymentId").val(),
           rbpStr = "#BillPayment-" + bpId,

           $fPayee = $("#Payee"),
           $fAmount = $("#Amount"),

           $rPayee = $(rbpStr + " .payee"),
           $rAmount = $(rbpStr + " .amount");

        year = $(rbpStr + " th").attr("data-search");
        $(rbpStr).removeClass("alert-danger").removeClass("alert-success").removeClass("alert-warning").addClass("alert-info");

        $fPayee.keyup(function () { $rPayee.text($(this).val()); })
           .focus(function () { $(this).select(); });

        $fAmount.keyup(function () { $rAmount.text('$' + intVal($(this).val()).format(2)); })
            .focus(function () { $(this).select(); });
    }

    $(".tab-pane .pagination a").click(function (e) {
        e.preventDefault();
        $(this).closest(".pagination").find("li").removeClass("active");
        $(this).parent("li").addClass("active");
        var selectedYear = $(this).attr("data-year");
        $(this).closest(".tab-pane").find("table tbody tr").removeClass("hide");
        if (selectedYear !== '') {
            $(this).closest(".tab-pane").find("table tbody tr:not([data-year='" + selectedYear + "'])").addClass("hide");
        }
    })
    $(".tab-pane .pagination li:first-child a").click();

    var $paymentFrequency = $("#PaymentFrequency"),
        $datePaidCheckbox = $("#IsDueDateStaysSame"),
        $amountCheckbox = $("#IsAmountStaysSame"),
        $explain = $("#explain");

    if ($paymentFrequency.length && $datePaidCheckbox.length && $amountCheckbox.length) {
        $paymentFrequency.on("change", function () {
            var value = $(this).val();
            if (value >= 3) {
                $datePaidCheckbox.removeAttr("disabled").parent().removeClass("disabled");
                $amountCheckbox.removeAttr("disabled").parent().removeClass("disabled");
                $explain.addClass("hide");
            } else {
                $datePaidCheckbox.attr({disabled: true, checked: true}).parent().addClass("disabled");
                $amountCheckbox.attr({ disabled: true, checked: true }).parent().addClass("disabled");
                $explain.removeClass("hide");
            }
        }).trigger("change");
    }

    $("input#IsShared").on("change", function () {
        if (this.checked) {
            $(".selectPayees").slideDown(400);
        } else {
            $(".selectPayees").slideUp(400);
        }
        console.log(this.checked);
        console.log('here');
    });

    $("[data-toggle=\"tooltip\"]").tooltip();
    $(".confirmation").confirmModal();

    $('.nav-tabs > li > a').on('click', function (e) {
        var target = $(this).attr('href');
        if (target.indexOf('gragh') > -1) {
            var chartNumber = $(this).attr('data-chart-number') - 1;
            if (charts[chartNumber].loaded === false) {
                setTimeout(function () {
                    var chart = new CanvasJS.Chart(charts[chartNumber].chartName, {
                        theme: "theme3",
                        animationEnabled: true,
                        animationDuration: 500,
                        data: [
                            {
                                type: "pie",
                                showInLegend: false,
                                toolTipContent: "{y} - #percent %",
                                yValueFormatString: "$#0.00",
                                legendText: "{indexLabel}",
                                dataPoints: charts[chartNumber].data
                            }
                        ]
                    });
                    chart.render();
                    charts[chartNumber].loaded = true;
                }, 250);
            }
        }
    });

    if (typeof (charts) !== 'undefined') {
        var chartCount = charts.length,
            i = 0;
        for (i = 0; i < chartCount; i++) {
            if (charts[i].loadOnStart === true) {
                var chart = new CanvasJS.Chart(charts[i].chartName, {
                    theme: "theme3",
                    animationEnabled: true,
                    animationDuration: 500,
                    data: [
                        {
                            type: "pie",
                            showInLegend: false,
                            toolTipContent: "{y} - #percent %",
                            yValueFormatString: "$#0.00",
                            legendText: "{indexLabel}",
                            dataPoints: charts[i].data
                        }
                    ]
                });
                chart.render();
                charts[i].loaded = true;
            }
        }
    }
});