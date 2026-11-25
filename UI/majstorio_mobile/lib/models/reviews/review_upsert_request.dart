class ReviewUpsertRequest {

ReviewUpsertRequest({required this.appointmentId, required this.rating, this.comment });


final int appointmentId;
final int rating;
final String? comment;

Map<String,dynamic> toJson(){
  return  {"rating" : rating, "appointmentId" : appointmentId, "comment" : comment};
}

}