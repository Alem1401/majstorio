import 'package:flutter/material.dart';
import 'package:majstorio_mobile/layouts/master_screen.dart';
import 'package:majstorio_mobile/models/appointment/appointment.dart';
import 'package:majstorio_mobile/models/appointment/appointment_search_request.dart';
import 'package:majstorio_mobile/models/auth/auth_session.dart';
import 'package:majstorio_mobile/models/offer/offer.dart';
import 'package:majstorio_mobile/providers/appointment/appointment_provider.dart';
import 'package:majstorio_mobile/providers/offer/offer_provider.dart';
import 'package:majstorio_mobile/providers/service_request/service_request_provider.dart';
import 'package:majstorio_mobile/screens/service_request/service_request_details_screen.dart';
import 'package:majstorio_mobile/theme/app_theme.dart';
import 'package:majstorio_mobile/utils/error_message.dart';
import 'package:majstorio_mobile/utils/formatters.dart';
import 'package:majstorio_mobile/widgets/common.dart';
import 'package:majstorio_mobile/widgets/dialogs.dart';
import 'package:majstorio_mobile/widgets/load_state.dart';
import 'package:majstorio_mobile/widgets/pager.dart';
import 'package:provider/provider.dart';

// The signed-in user's appointments (GET /Appointments/mine), newest start first.
// - default ("Termini"): every status, with a status filter.
// - active: true ("Aktivne usluge"): only Confirmed and Pending.
// - history: true ("Historija"): only Completed and Cancelled.
// An appointment only knows its offer, so the user's offers are loaded too: the offer gives the request
// (to open it) and the agreed price. If they cannot be loaded, the appointment is still shown, with a retry.
class AppointmentsScreen extends StatefulWidget {
  const AppointmentsScreen({super.key, this.active = false, this.history = false});

  final bool active;
  final bool history;

  @override
  State<AppointmentsScreen> createState() => _AppointmentsScreenState();
}

class _AppointmentsScreenState extends State<AppointmentsScreen> {
  late AppointmentProvider _appointmentProvider;
  late OfferProvider _offerProvider;
  late ServiceRequestProvider _serviceRequestProvider;

  var appointments = <Appointment>[];
  var isLoading = true;
  String? error;
  int? status;

  // Offer id -> offer, for the join described above. Null while not loaded.
  Map<int, Offer>? offersById;
  String? offersError;

  var page = 1;
  var pageSize = 10;
  var totalCount = 0;
  var _loadNumber = 0;

  Map<int?, String> get statusChoices {
    var names = Appointment.statusNames;
    if (widget.active) {
      return {Appointment.confirmed: names[Appointment.confirmed]!, Appointment.pending: names[Appointment.pending]!};
    }
    if (widget.history) {
      return {Appointment.completed: names[Appointment.completed]!, Appointment.cancelled: names[Appointment.cancelled]!};
    }
    return {null: "Svi", ...names};
  }

  String get title {
    if (widget.active) return "Aktivne usluge";
    if (widget.history) return "Historija poslova";
    return "Termini";
  }

  @override
  void initState() {
    _appointmentProvider = context.read<AppointmentProvider>();
    _offerProvider = context.read<OfferProvider>();
    _serviceRequestProvider = context.read<ServiceRequestProvider>();
    status = statusChoices.keys.first;
    // Accepting, cancelling or completing changes appointments.
    _appointmentProvider.addListener(loadAll);
    _offerProvider.addListener(loadAll);
    _serviceRequestProvider.addListener(loadAll);
    loadAll();
    super.initState();
  }

  @override
  void dispose() {
    _appointmentProvider.removeListener(loadAll);
    _offerProvider.removeListener(loadAll);
    _serviceRequestProvider.removeListener(loadAll);
    super.dispose();
  }

  Future<void> loadAll() => Future.wait([loadAppointments(), loadOffers()]);

  Future<void> loadAppointments() async {
    var loadNumber = ++_loadNumber;
    setState(() {
      isLoading = true;
      error = null;
    });
    try {
      var response = await _appointmentProvider.get(
        AppointmentSearchRequest(page: page, pageSize: pageSize, status: status),
      );
      if (!mounted || loadNumber != _loadNumber) return;
      setState(() {
        appointments = response.items;
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

  Future<void> loadOffers() async {
    setState(() {
      offersError = null;
    });
    try {
      var offers = await _offerProvider.getAllMine();
      if (!mounted) return;
      setState(() {
        offersById = {for (var offer in offers) offer.id: offer};
      });
    } catch (e) {
      if (!mounted) return;
      setState(() {
        offersError = exceptionText(e);
      });
    }
  }

  void openRequest(Appointment appointment) {
    var offer = offersById?[appointment.offerId];
    if (offer == null) {
      showMessage(context, "Zahtjev ovog termina nije pronađen. Osvježite listu.");
      loadOffers();
      return;
    }
    Navigator.push(
      context,
      MaterialPageRoute(builder: (context) => ServiceRequestDetailsScreen(requestId: offer.serviceRequestId)),
    );
  }

  Widget buildAppointmentCard(Appointment appointment) {
    var offer = offersById?[appointment.offerId];
    // The other person: the customer for a tradesperson, the tradesperson for a customer.
    var otherPerson = AuthSession.isTradesperson ? appointment.customerName : appointment.tradespersonName;

    return Card(
      child: InkWell(
        onTap: () => openRequest(appointment),
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  Expanded(
                    child: Text(
                      appointment.serviceCategoryName,
                      style: const TextStyle(fontSize: 16, fontWeight: FontWeight.w700, color: AppTheme.ink),
                    ),
                  ),
                  StatusChip(appointment.statusName, highlighted: appointment.status == Appointment.confirmed),
                ],
              ),
              const SizedBox(height: 8),
              Wrap(
                spacing: 16,
                runSpacing: 4,
                children: [
                  IconText(Icons.person_outline, otherPerson),
                  IconText(Icons.play_circle_outline, formatDateTime(appointment.startUtc)),
                  IconText(Icons.stop_circle_outlined, formatDateTime(appointment.endUtc)),
                  if (offer != null) IconText(Icons.euro, formatMoney(offer.price)),
                ],
              ),
            ],
          ),
        ),
      ),
    );
  }

  Widget buildResults() {
    if (isLoading && appointments.isEmpty) return const LoadingView();
    if (error != null) return ErrorView(message: error!, onRetry: loadAll);
    if (appointments.isEmpty) {
      return EmptyView(
        message: widget.history ? "Još nema završenih ni otkazanih poslova." : "Nema termina.",
        icon: Icons.event_busy_outlined,
      );
    }
    return Column(
      children: [
        if (offersError != null)
          Padding(
            padding: const EdgeInsets.only(bottom: 12),
            child: Card(
              child: ListTile(
                leading: const Icon(Icons.warning_amber_outlined, color: AppTheme.danger),
                title: const Text("Cijene i zahtjevi termina nisu učitani."),
                trailing: TextButton(onPressed: loadOffers, child: const Text("Ponovo")),
              ),
            ),
          ),
        for (var appointment in appointments)
          Padding(padding: const EdgeInsets.only(bottom: 12), child: buildAppointmentCard(appointment)),
        Card(
          child: Pager(
            page: page,
            pageSize: pageSize,
            totalCount: totalCount,
            onPageChanged: (newPage) {
              page = newPage;
              loadAppointments();
            },
          ),
        ),
      ],
    );
  }

  @override
  Widget build(BuildContext context) {
    return MasterScreen(
      title: title,
      child: RefreshIndicator(
        onRefresh: loadAll,
        child: ListView(
          padding: const EdgeInsets.all(16),
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
                          loadAppointments();
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
