import 'package:flutter/material.dart';
import 'package:majstorio_desktop/layouts/master_screen.dart';
import 'package:majstorio_desktop/models/payment/payment.dart';
import 'package:majstorio_desktop/models/payment/payment_search_request.dart';
import 'package:majstorio_desktop/providers/payment/payment_provider.dart';
import 'package:majstorio_desktop/theme/app_theme.dart';
import 'package:majstorio_desktop/utils/error_message.dart';
import 'package:majstorio_desktop/utils/formatters.dart';
import 'package:majstorio_desktop/widgets/dialogs.dart';
import 'package:majstorio_desktop/widgets/pager.dart';
import 'package:majstorio_desktop/widgets/status_filter.dart';
import 'package:provider/provider.dart';

// PayPal payments of completed jobs. A paid payment can be refunded in full once.
class PaymentsScreen extends StatefulWidget {
  const PaymentsScreen({super.key});

  @override
  State<PaymentsScreen> createState() => _PaymentsScreenState();
}

class _PaymentsScreenState extends State<PaymentsScreen> {
  late PaymentProvider _paymentProvider;
  var payments = <Payment>[];

  // Null shows payments in every status.
  int? status;

  var page = 1;
  var pageSize = 10;
  var totalCount = 0;

  void loadPayments() async {
    try {
      var response = await _paymentProvider.get(
        PaymentSearchRequest(page: page, pageSize: pageSize, status: status),
      );
      if (!mounted) return;
      setState(() {
        payments = response.items;
        totalCount = response.totalCount;
      });
    } catch (e) {
      if (!mounted) return;
      showMessage(context, "Payments could not be loaded.");
    }
  }

  void refund(Payment payment) async {
    var reason = await showDialog<String>(context: context, builder: (context) => _RefundDialog(payment: payment));
    if (reason == null || !mounted) return;

    var code = await _paymentProvider.refund(payment, reason);
    if (!mounted) return;
    if (code == 200) {
      showMessage(context, "The payment was refunded to the customer's PayPal account.");
      loadPayments();
    } else {
      showMessage(context, errorMessage("payment", code, conflict: "This payment was already refunded or PayPal refused it."));
    }
  }

  @override
  void initState() {
    _paymentProvider = context.read<PaymentProvider>();
    loadPayments();
    super.initState();
  }

  @override
  Widget build(BuildContext context) {
    return MasterScreen(
      title: "Payments",
      child: SingleChildScrollView(
        padding: const EdgeInsets.all(32),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            StatusFilter(
              label: "Status",
              names: Payment.statusNames,
              value: status,
              onChanged: (value) {
                status = value;
                page = 1;
                loadPayments();
              },
            ),
            const SizedBox(height: 24),
            Card(
              child: Column(
                children: [
                  LayoutBuilder(
                    builder: (context, constraints) {
                      return SingleChildScrollView(
                        scrollDirection: Axis.horizontal,
                        child: ConstrainedBox(
                          constraints: BoxConstraints(minWidth: constraints.maxWidth),
                          child: DataTable(
                            columns: const [
                              DataColumn(label: Text("Customer")),
                              DataColumn(label: Text("Tradesperson")),
                              DataColumn(label: Text("Amount")),
                              DataColumn(label: Text("Status")),
                              DataColumn(label: Text("Paid at")),
                              DataColumn(label: Text("Refund")),
                            ],
                            rows: payments.map((payment) {
                              return DataRow(
                                cells: [
                                  DataCell(Text(payment.customerName)),
                                  DataCell(Text(payment.tradespersonName)),
                                  DataCell(Text("${payment.amount.toStringAsFixed(2)} €")),
                                  DataCell(Text(payment.isRefunded ? "Refunded" : payment.statusName)),
                                  DataCell(Text(payment.paidAtUtc == null ? "-" : formatDateTime(payment.paidAtUtc!))),
                                  DataCell(
                                    IconButton(
                                      icon: const Icon(Icons.undo),
                                      color: AppTheme.danger,
                                      tooltip: payment.canBeRefunded
                                          ? "Refund in full"
                                          : "Only a paid payment that is not refunded yet can be refunded.",
                                      onPressed: payment.canBeRefunded ? () => refund(payment) : null,
                                    ),
                                  ),
                                ],
                              );
                            }).toList(),
                          ),
                        ),
                      );
                    },
                  ),
                  const Divider(),
                  Pager(
                    page: page,
                    pageSize: pageSize,
                    totalCount: totalCount,
                    onPageChanged: (newPage) {
                      page = newPage;
                      loadPayments();
                    },
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}

// Asks for the refund reason; returns it, or null when cancelled.
class _RefundDialog extends StatefulWidget {
  const _RefundDialog({required this.payment});

  final Payment payment;

  @override
  State<_RefundDialog> createState() => _RefundDialogState();
}

class _RefundDialogState extends State<_RefundDialog> {
  final _formKey = GlobalKey<FormState>();
  final _reason = TextEditingController();

  @override
  void dispose() {
    _reason.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return AlertDialog(
      title: const Text("Refund payment"),
      content: Form(
        key: _formKey,
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              "${widget.payment.amount.toStringAsFixed(2)} € will be returned to ${widget.payment.customerName}. "
              "This cannot be undone.",
            ),
            const SizedBox(height: 16),
            SizedBox(
              width: 400,
              child: TextFormField(
                controller: _reason,
                maxLength: 2000,
                decoration: const InputDecoration(labelText: "Reason"),
                validator: (value) =>
                    value == null || value.trim().isEmpty ? "Refund reason is required." : null,
              ),
            ),
          ],
        ),
      ),
      actions: [
        TextButton(onPressed: () => Navigator.pop(context), child: const Text("Cancel")),
        TextButton(
          onPressed: () {
            if (!_formKey.currentState!.validate()) return;
            Navigator.pop(context, _reason.text.trim());
          },
          child: const Text("Refund"),
        ),
      ],
    );
  }
}
