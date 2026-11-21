import 'package:majstorio_mobile/models/appointment/appointment.dart';
import 'package:majstorio_mobile/models/offer/offer.dart';
import 'package:majstorio_mobile/utils/formatters.dart';

// One row of the request list. Mirrors ServiceRequestResponse on the backend.
class ServiceRequest {
  // The status numbers the API uses for a request.
  static const int created = 0;
  static const int offerAccepted = 1;
  static const int appointmentConfirmed = 2;
  static const int completed = 3;
  static const int cancelled = 4;
  static const Map<int, String> statusNames = {
    0: 'Kreiran',
    1: 'Ponuda prihvaćena',
    2: 'Termin potvrđen',
    3: 'Završen',
    4: 'Otkazan',
  };

  final int id;
  final int customerId;
  final String customerName;
  final int cityId;
  final String cityName;
  final int serviceCategoryId;
  final String serviceCategoryName;
  // Set when the customer sent the request to one chosen tradesperson.
  final int? targetTradespersonProfileId;
  final String description;
  final String? address;
  final DateTime desiredStartUtc;
  final double maximumBudget;
  final int status;
  final DateTime createdAtUtc;

  ServiceRequest({
    required this.id,
    required this.customerId,
    required this.customerName,
    required this.cityId,
    required this.cityName,
    required this.serviceCategoryId,
    required this.serviceCategoryName,
    this.targetTradespersonProfileId,
    required this.description,
    this.address,
    required this.desiredStartUtc,
    required this.maximumBudget,
    required this.status,
    required this.createdAtUtc,
  });

  // An unknown number gets a neutral label, and no action is offered for it.
  String get statusName => statusNames[status] ?? 'Nepoznato';
  bool get isKnownStatus => statusNames.containsKey(status);

  // Completed and Cancelled are final: nothing can change the request any more.
  bool get isFinished => status == completed || status == cancelled;
  bool get canBeCancelled => isKnownStatus && !isFinished;

  factory ServiceRequest.fromJson(Map<String, dynamic> json) {
    return ServiceRequest(
      id: json['id'],
      customerId: json['customerId'],
      customerName: json['customerName'] ?? '',
      cityId: json['cityId'],
      cityName: json['cityName'] ?? '',
      serviceCategoryId: json['serviceCategoryId'],
      serviceCategoryName: json['serviceCategoryName'] ?? '',
      targetTradespersonProfileId: json['targetTradespersonProfileId'],
      description: json['description'] ?? '',
      address: json['address'],
      desiredStartUtc: parseUtc(json['desiredStartUtc']),
      maximumBudget: (json['maximumBudget'] as num).toDouble(),
      status: json['status'],
      createdAtUtc: parseUtc(json['createdAtUtc']),
    );
  }
}

// One recorded status change: who, when, from what and to what.
class StatusHistoryEntry {
  final int id;
  final String changedByName;
  final DateTime changedAtUtc;
  // Null on the first entry, when the request was created.
  final int? previousStatus;
  final int newStatus;
  final String? note;

  StatusHistoryEntry({
    required this.id,
    required this.changedByName,
    required this.changedAtUtc,
    this.previousStatus,
    required this.newStatus,
    this.note,
  });

  String get newStatusName => ServiceRequest.statusNames[newStatus] ?? 'Nepoznato';

  factory StatusHistoryEntry.fromJson(Map<String, dynamic> json) {
    return StatusHistoryEntry(
      id: json['id'],
      changedByName: json['changedByName'] ?? '',
      changedAtUtc: parseUtc(json['changedAtUtc']),
      previousStatus: json['previousStatus'],
      newStatus: json['newStatus'],
      note: json['note'],
    );
  }
}

// Everything the detail screen shows. Mirrors ServiceRequestDetailResponse on the backend.
// A customer sees all offers on their request; a tradesperson sees only their own.
class ServiceRequestDetail {
  final ServiceRequest request;
  final List<Offer> offers;
  // Null until an offer is accepted.
  final Appointment? appointment;
  final List<StatusHistoryEntry> statusHistory;

  ServiceRequestDetail({
    required this.request,
    required this.offers,
    this.appointment,
    required this.statusHistory,
  });

  // The offer the customer accepted; it holds the agreed price.
  Offer? get acceptedOffer {
    for (var offer in offers) {
      if (offer.status == Offer.accepted) return offer;
    }
    return null;
  }

  List<Offer> get pendingOffers => offers.where((offer) => offer.isPending).toList();

  factory ServiceRequestDetail.fromJson(Map<String, dynamic> json) {
    var history = ((json['statusHistory'] ?? []) as List)
        .map((entry) => StatusHistoryEntry.fromJson(entry))
        .toList();
    // The server's order: two changes can share a timestamp, so the id decides.
    history.sort((a, b) => a.id.compareTo(b.id));
    return ServiceRequestDetail(
      request: ServiceRequest.fromJson(json),
      offers: ((json['offers'] ?? []) as List).map((offer) => Offer.fromJson(offer)).toList(),
      appointment: json['appointment'] == null ? null : Appointment.fromJson(json['appointment']),
      statusHistory: history,
    );
  }
}
