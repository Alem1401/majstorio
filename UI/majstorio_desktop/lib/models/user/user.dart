import 'package:json_annotation/json_annotation.dart';

part 'user.g.dart';

@JsonSerializable()
class User {
  int? id;
  final String userName;
  final String email;
  final String? phoneNumber;
  final String firstName;
  final String lastName;
  final String? profileImageUrl;
  final bool isActive;
  // Customer, Tradesperson or Administrator.
  final String? role;

  User({
    this.id,
    required this.userName,
    required this.email,
    this.phoneNumber,
    required this.firstName,
    required this.lastName,
    this.profileImageUrl,
    this.isActive = true,
    this.role,
  });

  factory User.fromJson(Map<String, dynamic> json) => _$UserFromJson(json);

  Map<String, dynamic> toJson() => _$UserToJson(this);
}