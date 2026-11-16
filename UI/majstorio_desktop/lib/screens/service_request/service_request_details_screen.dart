import 'package:flutter/material.dart';
import 'package:majstorio_desktop/layouts/master_screen.dart';
import 'package:majstorio_desktop/models/service_request/service_request.dart';
import 'package:majstorio_desktop/providers/service_request/service_request_provider.dart';
import 'package:majstorio_desktop/theme/app_theme.dart';
import 'package:majstorio_desktop/utils/formatters.dart';
import 'package:majstorio_desktop/widgets/dialogs.dart';
import 'package:majstorio_desktop/utils/error_message.dart';
import 'package:provider/provider.dart';

// One request with its offers, the appointment and the status history. The admin can cancel it from here.
class ServiceRequestDetailsScreen extends StatefulWidget {
  const ServiceRequestDetailsScreen({super.key, required this.requestId});

  final int requestId;

  @override
  State<ServiceRequestDetailsScreen> createState() => _ServiceRequestDetailsScreenState();
}

class _ServiceRequestDetailsScreenState extends State<ServiceRequestDetailsScreen> {
  late ServiceRequestProvider _provider;

  // Null while the request is loading.
  ServiceRequestDetail? detail;

  // True after a cancel, so the list reloads when the admin goes back.
  var changed = false;

  void loadRequest() async {
    var response = await _provider.getById(widget.requestId);
    if (!mounted) return;
    setState(() {
      detail = response;
    });
  }

  void cancelRequest() async {
    var confirmed = await showConfirmDialog(
      context,
      "Are you sure you want to cancel this request? Its appointment and open offers are cancelled too.",
    );
    if (!confirmed) return;
    var statusCode = await _provider.cancel(widget.requestId);
    if (!mounted) return;
    if (statusCode == 200) {
      changed = true;
      showMessage(context, "The request was cancelled.");
      loadRequest();
    } else {
      // For example: a completed request cannot be cancelled.
      showMessage(context, errorMessage("request", statusCode, conflict: "A completed or cancelled request cannot be cancelled."));
    }
  }

  @override
  void initState() {
    _provider = context.read<ServiceRequestProvider>();
    loadRequest();
    super.initState();
  }

  Widget sectionTitle(String text) {
    return Padding(
      padding: const EdgeInsets.fromLTRB(24, 20, 24, 12),
      child: Text(
        text,
        style: const TextStyle(fontSize: 18, fontWeight: FontWeight.w700, color: AppTheme.ink),
      ),
    );
  }

  Widget field(String label, String value) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 8),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          SizedBox(
            width: 140,
            child: Text(label, style: const TextStyle(color: AppTheme.textMuted)),
          ),
          Expanded(child: Text(value)),
        ],
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return PopScope(
      // Hands "changed" back to the list, also when the window's back arrow is used.
      canPop: false,
      onPopInvokedWithResult: (didPop, result) {
        if (didPop) return;
        Navigator.pop(context, changed);
      },
      child: MasterScreen(
        title: "Request details",
        child: detail == null
            ? const Center(child: CircularProgressIndicator())
            : buildDetail(detail!),
      ),
    );
  }

  Widget buildDetail(ServiceRequestDetail detail) {
    var request = detail.request;
    return SingleChildScrollView(
      padding: const EdgeInsets.all(32),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Card(
            child: Padding(
              padding: const EdgeInsets.all(24),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    children: [
                      Expanded(
                        child: Text(
                          "${request.serviceCategoryName} in ${request.cityName}",
                          style: const TextStyle(
                            fontSize: 20,
                            fontWeight: FontWeight.w700,
                            color: AppTheme.ink,
                          ),
                        ),
                      ),
                      if (request.canBeCancelled)
                        ElevatedButton.icon(
                          onPressed: cancelRequest,
                          icon: const Icon(Icons.cancel_outlined),
                          label: const Text("Cancel request"),
                        ),
                    ],
                  ),
                  const SizedBox(height: 16),
                  field("Status", request.statusName),
                  field("Customer", request.customerName),
                  field("Description", request.description),
                  field("Address", request.address ?? ""),
                  field("Desired start", formatDateTime(request.desiredStartUtc)),
                  field("Maximum budget", "${request.maximumBudget.toStringAsFixed(2)} EUR"),
                  field("Created", formatDateTime(request.createdAtUtc)),
                  if (detail.appointment != null)
                    field(
                      "Appointment",
                      "${formatDateTime(detail.appointment!.startUtc)} - ${formatDateTime(detail.appointment!.endUtc)}, "
                          "${detail.appointment!.tradespersonName} (${detail.appointment!.statusName})",
                    ),
                ],
              ),
            ),
          ),
          const SizedBox(height: 24),
          Card(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                sectionTitle("Offers"),
                SizedBox(
                  width: double.infinity,
                  child: DataTable(
                    columns: const [
                      DataColumn(label: Text("Tradesperson")),
                      DataColumn(label: Text("Price")),
                      DataColumn(label: Text("Proposed start")),
                      DataColumn(label: Text("Duration")),
                      DataColumn(label: Text("Status")),
                    ],
                    rows: detail.offers.map((offer) {
                      return DataRow(
                        cells: [
                          DataCell(Text(offer.tradespersonName)),
                          DataCell(Text("${offer.price.toStringAsFixed(2)} EUR")),
                          DataCell(Text(formatDateTime(offer.proposedStartUtc))),
                          DataCell(Text("${offer.durationMinutes} min")),
                          DataCell(Text(offer.statusName)),
                        ],
                      );
                    }).toList(),
                  ),
                ),
              ],
            ),
          ),
          const SizedBox(height: 24),
          Card(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                sectionTitle("Status history"),
                SizedBox(
                  width: double.infinity,
                  child: DataTable(
                    columns: const [
                      DataColumn(label: Text("When")),
                      DataColumn(label: Text("Changed by")),
                      DataColumn(label: Text("From")),
                      DataColumn(label: Text("To")),
                      DataColumn(label: Text("Note")),
                    ],
                    rows: detail.statusHistory.map((entry) {
                      return DataRow(
                        cells: [
                          DataCell(Text(formatDateTime(entry.changedAtUtc))),
                          DataCell(Text(entry.changedByName)),
                          DataCell(Text(ServiceRequest.statusNames[entry.previousStatus] ?? "")),
                          DataCell(Text(ServiceRequest.statusNames[entry.newStatus] ?? "")),
                          DataCell(Text(entry.note ?? "")),
                        ],
                      );
                    }).toList(),
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}
