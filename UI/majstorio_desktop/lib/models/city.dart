class City {
   int? id;
   final String name;
   final bool isActive;

   City({this.id, required this.name,this.isActive = true });

  factory City.fromJson(Map<String, dynamic> json) {
  return City(
    id: json['id'],
    name: json['name'],
    isActive: json['isActive'],
  );

  
}
  static Map<String, dynamic> toJson(City city) {
    return {
      'name': city.name,
      'isActive': city.isActive,
    };
  }

}