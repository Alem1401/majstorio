import 'package:flutter/material.dart';
import 'package:majstorio_mobile/layouts/master_screen.dart';
import 'package:majstorio_mobile/models/appointment/appointment.dart';
import 'package:majstorio_mobile/models/auth/auth_session.dart';
import 'package:majstorio_mobile/models/offer/offer.dart';
import 'package:majstorio_mobile/models/review/review.dart';
import 'package:majstorio_mobile/models/service_request/service_request.dart';
import 'package:majstorio_mobile/providers/appointment/appointment_provider.dart';
import 'package:majstorio_mobile/providers/offer/offer_provider.dart';
import 'package:majstorio_mobile/providers/review/review_provider.dart';
import 'package:majstorio_mobile/providers/service_request/service_request_provider.dart';
import 'package:majstorio_mobile/screens/offer/offer_details_screen.dart';
import 'package:majstorio_mobile/screens/offer/offers_compare_screen.dart';
import 'package:majstorio_mobile/screens/review/review_form_screen.dart';
import 'package:majstorio_mobile/screens/service_request/service_request_form_screen.dart';
import 'package:majstorio_mobile/theme/app_theme.dart';
import 'package:majstorio_mobile/utils/error_message.dart';
import 'package:majstorio_mobile/utils/formatters.dart';
import 'package:majstorio_mobile/widgets/common.dart';
import 'package:majstorio_mobile/widgets/dialogs.dart';
import 'package:majstorio_mobile/widgets/load_state.dart';
import 'package:provider/provider.dart';

// One request with its offers, appointment and status history, and the actions the signed-in user may take.
// The same screen serves the customer and the tradesperson; the buttons depend on who is looking.
// The buttons are only a guide: the API checks every action again, and a refused action reloads the screen.
class ServiceRequestDetailsScreen extends StatefulWidget {
  const ServiceRequestDetailsScreen({super.key, required this.requestId});

  final int requestId;

  @override
  State<ServiceRequestDetailsScreen> createState() => _ServiceRequestDetailsScreenState();
}

class _ServiceRequestDetailsScreenState extends State<ServiceRequestDetailsScreen> {
  late ServiceRequestProvider _serviceRequestProvider;
  late OfferProvider _offerProvider;
  late AppointmentProvider _appointmentProvider;
  late ReviewProvider _reviewProvider;

  ServiceRequestDetail? detail;
  // The customer's rating of the completed job; loaded only for the owner of a completed job.
  Review? review;
  var isReviewLoaded = false;
  String? reviewError;
  String? error;
  // True while an action is running, so no second action can start.
  var isBusy = false;
  var _loadNumber = 0;

  @override
  void initState() {
    _serviceRequestProvider = context.read<ServiceRequestProvider>();
    _offerProvider = context.read<OfferProvider>();
    _appointmentProvider = context.read<AppointmentProvider>();
    _reviewProvider = context.read<ReviewProvider>();
    _serviceRequestProvider.addListener(loadDetail);
    _offerProvider.addListener(loadDetail);
    _appointmentProvider.addListener(loadDetail);
    loadDetail();
    super.initState();
  }

  @override
  void dispose() {
    _serviceRequestProvider.removeListener(loadDetail);
    _offerProvider.removeListener(loadDetail);
    _appointmentProvider.removeListener(loadDetail);
    super.dispose();
  }

  Future<void> loadDetail() async {
    var loadNumber = ++_loadNumber;
    setState(() {
      error = null;
    });
    try {
      var result = await _serviceRequestProvider.getById(widget.requestId);
      if (!mounted || loadNumber != _loadNumber) return;
      setState(() {
        detail = result;
      });
      if (canBeRated(result)) loadReview(result.appointment!.id);
    } catch (e) {
      if (!mounted || loadNumber != _loadNumber) return;
      setState(() {
        // 404: the request does not exist or this user may not see it. Nothing about it is shown.
        error = e is ApiException && e.statusCode == 404 ? "Ovaj zahtjev nije dostupan." : exceptionText(e);
        if (e is ApiException && e.statusCode == 404) detail = null;
      });
    }
  }

  // A customer rates a job once it is completed. The API checks ownership and status again.
  bool canBeRated(ServiceRequestDetail detail) =>
      isCustomerOwner(detail.request) && detail.appointment?.status == Appointment.completed;

  Future<void> loadReview(int appointmentId) async {
    setState(() {
      reviewError = null;
    });
    try {
      var result = await _reviewProvider.getForAppointment(appointmentId);
      if (!mounted) return;
      setState(() {
        review = result;
        isReviewLoaded = true;
      });
    } catch (e) {
      if (!mounted) return;
      setState(() {
        reviewError = exceptionText(e);
      });
    }
  }

  void openReviewForm(Appointment appointment) async {
    var saved = await Navigator.push<bool>(
      context,
      MaterialPageRoute(builder: (context) => ReviewFormScreen(appointment: appointment)),
    );
    if (saved == true) loadReview(appointment.id);
  }

  // Runs an action, shows its result, and reloads the request. A refused action (for example 409,
  // because the request changed meanwhile) shows the reason and reloads, so the screen shows the real state.
  Future<void> runAction(Future<void> Function() action, String successMessage, {bool closeAfter = false}) async {
    setState(() {
      isBusy = true;
    });
    try {
      await action();
      if (!mounted) return;
      showMessage(context, successMessage);
      if (closeAfter) {
        Navigator.pop(context, true);
        return;
      }
    } catch (e) {
      if (!mounted) return;
      showMessage(context, exceptionText(e));
    }
    if (!mounted) return;
    setState(() {
      isBusy = false;
    });
    loadDetail();
  }

  // ---- Who is looking -------------------------------------------------------------------------

  bool isCustomerOwner(ServiceRequest request) =>
      AuthSession.isCustomer && request.customerId == AuthSession.userId;

  // A tradesperson only sees their own offers, so any offer in the list is theirs.
  Offer? myOffer(ServiceRequestDetail detail) =>
      AuthSession.isTradesperson && detail.offers.isNotEmpty ? detail.offers.first : null;

  bool isAssignedTradesperson(ServiceRequestDetail detail) =>
      AuthSession.isTradesperson && myOffer(detail)?.status == Offer.accepted;

  // ---- Actions ---------------------------------------------------------------------------------

  void editRequest(ServiceRequest request) async {
    var saved = await Navigator.push<bool>(
      context,
      MaterialPageRoute(builder: (context) => ServiceRequestFormScreen(toUpdate: request)),
    );
    if (saved == true) loadDetail();
  }

  void deleteRequest(ServiceRequest request) async {
    var confirmed = await showConfirmDialog(
      context,
      "Zahtjev će biti trajno obrisan zajedno sa historijom. Ova radnja se ne može poništiti. Obrisati zahtjev?",
      title: "Brisanje zahtjeva",
    );
    if (!confirmed) return;
    await runAction(
      () => _serviceRequestProvider.delete(request.id),
      "Zahtjev je obrisan.",
      closeAfter: true,
    );
  }

  void cancelRequest(ServiceRequest request) async {
    var reason = await showReasonDialog(
      context,
      "Zahtjev će biti otkazan. Otvorene ponude se zatvaraju, a dogovoreni termin se otkazuje. Otkazati zahtjev?",
    );
    if (reason == null) return;
    await runAction(() => _serviceRequestProvider.cancel(request.id, reason), "Zahtjev je otkazan.");
  }

  void openCompare(ServiceRequestDetail detail) async {
    var accepted = await Navigator.push<bool>(
      context,
      MaterialPageRoute(builder: (context) => OffersCompareScreen(detail: detail)),
    );
    if (accepted == true) loadDetail();
  }

  void openOfferForm(ServiceRequest request, {Offer? toUpdate}) async {
    var saved = await Navigator.push<bool>(
      context,
      MaterialPageRoute(builder: (context) => OfferDetailsScreen(request: request, toUpdate: toUpdate)),
    );
    if (saved == true) loadDetail();
  }

  void withdrawOffer(Offer offer) async {
    var confirmed = await showConfirmDialog(
      context,
      "Ponuda će biti povučena i ostaje zabilježena kao povučena. Novu ponudu za ovaj zahtjev nije moguće poslati. Povući ponudu?",
    );
    if (!confirmed) return;
    await runAction(() => _offerProvider.withdraw(offer.id), "Ponuda je povučena.");
  }

  void completeAppointment(Appointment appointment) async {
    var confirmed = await showConfirmDialog(context, "Označiti posao kao završen?");
    if (!confirmed) return;
    await runAction(() => _appointmentProvider.complete(appointment.id), "Posao je označen kao završen.");
  }

  // ---- Sections --------------------------------------------------------------------------------

  Widget buildSummary(ServiceRequest request) {
    return Card(
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
                    style: const TextStyle(fontSize: 18, fontWeight: FontWeight.w700, color: AppTheme.ink),
                  ),
                ),
                StatusChip(request.statusName, highlighted: !request.isFinished),
              ],
            ),
            const SizedBox(height: 12),
            Text(request.description, style: const TextStyle(fontSize: 15)),
            const SizedBox(height: 12),
            const Divider(),
            const SizedBox(height: 4),
            InfoRow(icon: Icons.person_outline, label: "Korisnik", value: request.customerName),
            InfoRow(icon: Icons.place_outlined, label: "Grad", value: request.cityName),
            if (request.address != null && request.address!.isNotEmpty)
              InfoRow(icon: Icons.home_outlined, label: "Adresa", value: request.address!),
            InfoRow(icon: Icons.schedule, label: "Željeni početak", value: formatDateTime(request.desiredStartUtc)),
            InfoRow(icon: Icons.euro, label: "Najveći budžet", value: formatMoney(request.maximumBudget)),
            InfoRow(icon: Icons.calendar_today_outlined, label: "Kreiran", value: formatDateTime(request.createdAtUtc)),
            if (request.targetTradespersonProfileId != null)
              const InfoRow(icon: Icons.person_pin_outlined, label: "Vrsta", value: "Zahtjev za odabranog majstora"),
          ],
        ),
      ),
    );
  }

  Widget buildOfferCard(Offer offer) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Expanded(
                  child: Text(
                    offer.tradespersonName,
                    style: const TextStyle(fontWeight: FontWeight.w700, color: AppTheme.ink),
                  ),
                ),
                StatusChip(offer.statusName, highlighted: offer.isPending || offer.status == Offer.accepted),
              ],
            ),
            const SizedBox(height: 8),
            InfoRow(icon: Icons.euro, label: "Cijena", value: formatMoney(offer.price)),
            InfoRow(icon: Icons.schedule, label: "Predloženi početak", value: formatDateTime(offer.proposedStartUtc)),
            InfoRow(icon: Icons.timer_outlined, label: "Trajanje", value: formatDuration(offer.durationMinutes)),
            if (offer.decisionReason != null && offer.decisionReason!.isNotEmpty)
              InfoRow(icon: Icons.info_outline, label: "Razlog", value: offer.decisionReason!),
          ],
        ),
      ),
    );
  }

  Widget buildAppointment(ServiceRequestDetail detail) {
    var appointment = detail.appointment!;
    var agreedOffer = detail.acceptedOffer;
    var canComplete = isAssignedTradesperson(detail) && appointment.canBeCompleted(DateTime.now());
    var waitingForStart = isAssignedTradesperson(detail) &&
        appointment.status == Appointment.confirmed &&
        !appointment.canBeCompleted(DateTime.now());

    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                const Expanded(
                  child: Text("Termin", style: TextStyle(fontWeight: FontWeight.w700, color: AppTheme.ink)),
                ),
                StatusChip(appointment.statusName, highlighted: appointment.status == Appointment.confirmed),
              ],
            ),
            const SizedBox(height: 8),
            InfoRow(icon: Icons.engineering_outlined, label: "Majstor", value: appointment.tradespersonName),
            InfoRow(icon: Icons.play_circle_outline, label: "Početak", value: formatDateTime(appointment.startUtc)),
            InfoRow(icon: Icons.stop_circle_outlined, label: "Kraj", value: formatDateTime(appointment.endUtc)),
            if (agreedOffer != null)
              InfoRow(icon: Icons.euro, label: "Dogovorena cijena", value: formatMoney(agreedOffer.price)),
            if (canComplete) ...[
              const SizedBox(height: 12),
              SizedBox(
                width: double.infinity,
                child: ElevatedButton.icon(
                  onPressed: isBusy ? null : () => completeAppointment(appointment),
                  icon: const Icon(Icons.task_alt),
                  label: const Text("Završi posao"),
                ),
              ),
            ],
            if (waitingForStart) ...[
              const SizedBox(height: 12),
              const SizedBox(
                width: double.infinity,
                child: ElevatedButton(onPressed: null, child: Text("Završi posao")),
              ),
              const SizedBox(height: 6),
              const Text(
                "Posao se može označiti kao završen nakon početka termina.",
                style: TextStyle(color: AppTheme.textMuted),
              ),
            ],
          ],
        ),
      ),
    );
  }

  Widget buildReview(ServiceRequestDetail detail) {
    var appointment = detail.appointment!;
    if (reviewError != null) {
      return ErrorView(message: reviewError!, onRetry: () => loadReview(appointment.id));
    }
    if (!isReviewLoaded) return const LoadingView();
    var existing = review;
    if (existing == null) {
      return SizedBox(
        width: double.infinity,
        child: ElevatedButton.icon(
          onPressed: isBusy ? null : () => openReviewForm(appointment),
          icon: const Icon(Icons.star_outline),
          label: const Text("Ocijeni majstora"),
        ),
      );
    }
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                for (var star = 1; star <= 5; star++)
                  Icon(star <= existing.rating ? Icons.star : Icons.star_border, color: AppTheme.gold),
                const SizedBox(width: 8),
                Text("${existing.rating}/5", style: const TextStyle(fontWeight: FontWeight.w700, color: AppTheme.ink)),
              ],
            ),
            if (existing.comment != null && existing.comment!.isNotEmpty) ...[
              const SizedBox(height: 8),
              Text(existing.comment!),
            ],
            const SizedBox(height: 6),
            Text(
              "Ocijenjeno ${formatDate(existing.createdAtUtc)}",
              style: const TextStyle(fontSize: 12, color: AppTheme.textMuted),
            ),
          ],
        ),
      ),
    );
  }

  Widget buildHistory(List<StatusHistoryEntry> history) {
    if (history.isEmpty) {
      return const Text("Nema zapisa.", style: TextStyle(color: AppTheme.textMuted));
    }
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          children: [
            for (var i = 0; i < history.length; i++)
              IntrinsicHeight(
                child: Row(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    // The vertical line with a dot for each step.
                    SizedBox(
                      width: 24,
                      child: Column(
                        children: [
                          Container(
                            width: 12,
                            height: 12,
                            margin: const EdgeInsets.only(top: 4),
                            decoration: BoxDecoration(
                              color: i == history.length - 1 ? AppTheme.gold : AppTheme.ink,
                              shape: BoxShape.circle,
                            ),
                          ),
                          if (i < history.length - 1)
                            Expanded(child: Container(width: 2, color: AppTheme.border)),
                        ],
                      ),
                    ),
                    const SizedBox(width: 8),
                    Expanded(
                      child: Padding(
                        padding: const EdgeInsets.only(bottom: 16),
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text(
                              history[i].newStatusName,
                              style: const TextStyle(fontWeight: FontWeight.w700, color: AppTheme.ink),
                            ),
                            const SizedBox(height: 2),
                            Text(
                              "${formatDateTime(history[i].changedAtUtc)} · ${history[i].changedByName}",
                              style: const TextStyle(fontSize: 13, color: AppTheme.textMuted),
                            ),
                            if (history[i].note != null && history[i].note!.isNotEmpty) ...[
                              const SizedBox(height: 4),
                              Text(history[i].note!),
                            ],
                          ],
                        ),
                      ),
                    ),
                  ],
                ),
              ),
          ],
        ),
      ),
    );
  }

  // The buttons the signed-in user may use on this request.
  List<Widget> buildActions(ServiceRequestDetail detail) {
    var request = detail.request;
    var buttons = <Widget>[];
    // An unknown status from a newer API: show the data, offer no action.
    if (!request.isKnownStatus) return buttons;

    if (isCustomerOwner(request)) {
      if (request.status == ServiceRequest.created && detail.pendingOffers.isNotEmpty) {
        buttons.add(ElevatedButton.icon(
          onPressed: isBusy ? null : () => openCompare(detail),
          icon: const Icon(Icons.compare_arrows),
          label: Text("Usporedi ponude (${detail.pendingOffers.length})"),
        ));
      }
      if (request.status == ServiceRequest.created && detail.offers.isEmpty) {
        buttons.add(OutlinedButton.icon(
          onPressed: isBusy ? null : () => editRequest(request),
          icon: const Icon(Icons.edit_outlined),
          label: const Text("Uredi zahtjev"),
        ));
        buttons.add(OutlinedButton.icon(
          style: OutlinedButton.styleFrom(foregroundColor: AppTheme.danger),
          onPressed: isBusy ? null : () => deleteRequest(request),
          icon: const Icon(Icons.delete_outline),
          label: const Text("Obriši zahtjev"),
        ));
      }
    }

    if (AuthSession.isTradesperson) {
      var offer = myOffer(detail);
      var isOwnRequest = request.customerId == AuthSession.userId;
      if (offer == null && request.status == ServiceRequest.created && !isOwnRequest) {
        buttons.add(ElevatedButton.icon(
          onPressed: isBusy ? null : () => openOfferForm(request),
          icon: const Icon(Icons.request_quote_outlined),
          label: const Text("Pošalji ponudu"),
        ));
      }
      if (offer != null && offer.isPending && request.status == ServiceRequest.created) {
        buttons.add(ElevatedButton.icon(
          onPressed: isBusy ? null : () => openOfferForm(request, toUpdate: offer),
          icon: const Icon(Icons.edit_outlined),
          label: const Text("Izmijeni ponudu"),
        ));
        buttons.add(OutlinedButton.icon(
          style: OutlinedButton.styleFrom(foregroundColor: AppTheme.danger),
          onPressed: isBusy ? null : () => withdrawOffer(offer),
          icon: const Icon(Icons.undo),
          label: const Text("Povuci ponudu"),
        ));
      }
    }

    // The owner and the assigned tradesperson can cancel until the request is finished.
    if (request.canBeCancelled && (isCustomerOwner(request) || isAssignedTradesperson(detail))) {
      buttons.add(OutlinedButton.icon(
        style: OutlinedButton.styleFrom(foregroundColor: AppTheme.danger),
        onPressed: isBusy ? null : () => cancelRequest(request),
        icon: const Icon(Icons.cancel_outlined),
        label: const Text("Otkaži zahtjev"),
      ));
    }
    return buttons;
  }

  Widget buildContent(ServiceRequestDetail detail) {
    var request = detail.request;
    var actions = buildActions(detail);
    var offersTitle = AuthSession.isTradesperson ? "Vaša ponuda" : "Ponude";

    return ListView(
      padding: const EdgeInsets.all(16),
      children: [
        buildSummary(request),
        if (actions.isNotEmpty) ...[
          const SizedBox(height: 16),
          for (var button in actions)
            Padding(
              padding: const EdgeInsets.only(bottom: 8),
              child: SizedBox(width: double.infinity, child: button),
            ),
        ],
        if (isCustomerOwner(request) &&
            request.status == ServiceRequest.created &&
            detail.offers.isNotEmpty &&
            detail.pendingOffers.isEmpty)
          const Padding(
            padding: EdgeInsets.only(top: 8),
            child: Text(
              "Zahtjev ima ponude, pa se više ne može uređivati ni brisati. Može se otkazati.",
              style: TextStyle(color: AppTheme.textMuted),
            ),
          ),
        if (detail.appointment != null) ...[
          const SectionTitle("Termin"),
          buildAppointment(detail),
        ],
        if (canBeRated(detail)) ...[
          const SectionTitle("Vaša ocjena"),
          buildReview(detail),
        ],
        SectionTitle(offersTitle),
        if (detail.offers.isEmpty)
          Text(
            AuthSession.isTradesperson ? "Niste poslali ponudu za ovaj zahtjev." : "Još nema ponuda.",
            style: const TextStyle(color: AppTheme.textMuted),
          )
        else
          for (var offer in detail.offers)
            Padding(padding: const EdgeInsets.only(bottom: 8), child: buildOfferCard(offer)),
        const SectionTitle("Historija statusa"),
        buildHistory(detail.statusHistory),
        const SizedBox(height: 24),
      ],
    );
  }

  @override
  Widget build(BuildContext context) {
    Widget body;
    if (error != null && detail == null) {
      body = ErrorView(message: error!, onRetry: loadDetail);
    } else if (detail == null) {
      body = const LoadingView();
    } else {
      body = RefreshIndicator(onRefresh: loadDetail, child: buildContent(detail!));
    }
    return MasterScreen(title: "Zahtjev", child: body);
  }
}
