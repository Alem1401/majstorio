// What the app sends to update a review (PUT /Reviews/{id}). No id: it goes in the URL.
// Customers create reviews in the mobile app, so the desktop app only updates. Mirrors ReviewUpdateRequest.
class ReviewUpsertRequest {
  final int appointmentId;
  final int rating;
  final String? comment;

  ReviewUpsertRequest({required this.appointmentId, required this.rating, this.comment});

  Map<String, dynamic> toJson() => {
    'appointmentId': appointmentId,
    'rating': rating,
    'comment': comment,
  };
}
