import 'package:flutter/material.dart';
import 'package:majstorio_mobile/layouts/master_screen.dart';
import 'package:majstorio_mobile/models/service_request/service_request.dart';
import 'package:majstorio_mobile/models/service_request/service_request_search_request.dart';
import 'package:majstorio_mobile/providers/offer/offer_provider.dart';
import 'package:majstorio_mobile/providers/service_request/service_request_provider.dart';
import 'package:majstorio_mobile/screens/service_request/service_request_details_screen.dart';
import 'package:majstorio_mobile/screens/service_request/service_request_form_screen.dart';
import 'package:majstorio_mobile/theme/app_theme.dart';
import 'package:majstorio_mobile/utils/error_message.dart';
import 'package:majstorio_mobile/utils/formatters.dart';
import 'package:majstorio_mobile/widgets/common.dart';
import 'package:majstorio_mobile/widgets/load_state.dart';
import 'package:majstorio_mobile/widgets/pager.dart';
import 'package:provider/provider.dart';

// The customer's requests ("Zahtjevi"), newest first, filtered by one status at a time.
// With history: true it shows only finished requests (Completed and Cancelled): the customer's history.
class ServiceRequestsScreen extends StatefulWidget {
  const ServiceRequestsScreen({super.key, this.history = false});

  final bool history;

  @override
  State<ServiceRequestsScreen> createState() => _ServiceRequestsScreenState();
}

class _ServiceRequestsScreenState extends State<ServiceRequestsScreen> {
  late ServiceRequestProvider _serviceRequestProvider;
  late OfferProvider _offerProvider;

  var requests = <ServiceRequest>[];
  var isLoading = true;
  String? error;

  // The API filters by one status; null means every status (only on the main list).
  int? status;

  var page = 1;
  var pageSize = 10;
  var totalCount = 0;
  var _loadNumber = 0;

  // The statuses the chips offer.
  Map<int?, String> get statusChoices => widget.history
      ? {
          ServiceRequest.completed: ServiceRequest.statusNames[ServiceRequest.completed]!,
          ServiceRequest.cancelled: ServiceRequest.statusNames[ServiceRequest.cancelled]!,
        }
      : {null: "Svi", ...ServiceRequest.statusNames};

  @override
  void initState() {
    _serviceRequestProvider = context.read<ServiceRequestProvider>();
    _offerProvider = context.read<OfferProvider>();
    status = widget.history ? ServiceRequest.completed : null;
    // Any change to a request or an offer can change this list.
    _serviceRequestProvider.addListener(loadRequests);
    _offerProvider.addListener(loadRequests);
    loadRequests();
    super.initState();
  }

  @override
  void dispose() {
    _serviceRequestProvider.removeListener(loadRequests);
    _offerProvider.removeListener(loadRequests);
    super.dispose();
  }

  Future<void> loadRequests() async {
    var loadNumber = ++_loadNumber;
    setState(() {
      isLoading = true;
      error = null;
    });
    try {
      var response = await _serviceRequestProvider.get(
        ServiceRequestSearchRequest(page: page, pageSize: pageSize, status: status),
      );
      if (!mounted || loadNumber != _loadNumber) return;
      setState(() {
        requests = response.items;
        totalCount = response.totalCount;
        isLoading = false;
      });
    } catch (e) {
      if (!mounted || loadNumber != _loadNumber) return;
      setState(() {
        error = exceptionText(e);
        isLoading = false;
      });
    }
  }

  void openCreate() async {
    var created = await Navigator.push<bool>(
      context,
      MaterialPageRoute(builder: (context) => const ServiceRequestFormScreen()),
    );
    if (created == true) {
      // The new request is the newest one: show the first page without a filter so it is visible at the top.
      setState(() {
        status = null;
      });
      page = 1;
      loadRequests();
    }
  }

  Widget buildRequestCard(ServiceRequest request) {
    return Card(
      child: InkWell(
        onTap: () {
          Navigator.push(
            context,
            MaterialPageRoute(builder: (context) => ServiceRequestDetailsScreen(requestId: request.id)),
          );
        },
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  Expanded(
                    child: Text(
                      request.serviceCategoryName,
                      style: const TextStyle(fontSize: 16, fontWeight: FontWeight.w700, color: AppTheme.ink),
                    ),
                  ),
                  StatusChip(request.statusName, highlighted: !request.isFinished),
                ],
              ),
              const SizedBox(height: 6),
              Text(request.description, maxLines: 2, overflow: TextOverflow.ellipsis),
              const SizedBox(height: 8),
              Wrap(
                spacing: 16,
                runSpacing: 4,
                children: [
                  IconText(Icons.place_outlined, request.cityName),
                  IconText(Icons.schedule, formatDateTime(request.desiredStartUtc)),
                  IconText(Icons.euro, "do ${formatMoney(request.maximumBudget)}"),
                ],
              ),
            ],
          ),
        ),
      ),
    );
  }

  Widget buildResults() {
    if (isLoading && requests.isEmpty) return const LoadingView();
    if (error != null) return ErrorView(message: error!, onRetry: loadRequests);
    if (requests.isEmpty) {
      return EmptyView(
        message: status == null ? "Još nemate zahtjeva." : "Nema zahtjeva sa ovim statusom.",
        icon: Icons.assignment_outlined,
      );
    }
    return Column(
      children: [
        for (var request in requests)
          Padding(padding: const EdgeInsets.only(bottom: 12), child: buildRequestCard(request)),
        Card(
          child: Pager(
            page: page,
            pageSize: pageSize,
            totalCount: totalCount,
            onPageChanged: (newPage) {
              page = newPage;
              loadRequests();
            },
          ),
        ),
      ],
    );
  }

  @override
  Widget build(BuildContext context) {
    return MasterScreen(
      title: widget.history ? "Historija zahtjeva" : "Zahtjevi",
      floatingActionButton: widget.history
          ? null
          : FloatingActionButton.extended(
              backgroundColor: AppTheme.ink,
              foregroundColor: Colors.white,
              onPressed: openCreate,
              icon: const Icon(Icons.add),
              label: const Text("Novi zahtjev"),
            ),
      child: RefreshIndicator(
        onRefresh: loadRequests,
        child: ListView(
          padding: const EdgeInsets.fromLTRB(16, 16, 16, 96),
          children: [
            SingleChildScrollView(
              scrollDirection: Axis.horizontal,
              child: Row(
                children: [
                  for (var choice in statusChoices.entries)
                    Padding(
                      padding: const EdgeInsets.only(right: 8),
                      child: ChoiceChip(
                        label: Text(choice.value),
                        selected: status == choice.key,
                        onSelected: (selected) {
                          if (!selected) return;
                          setState(() {
                            status = choice.key;
                          });
                          page = 1;
                          loadRequests();
                        },
                      ),
                    ),
                ],
              ),
            ),
            const SizedBox(height: 16),
            buildResults(),
          ],
        ),
      ),
    );
  }
}
