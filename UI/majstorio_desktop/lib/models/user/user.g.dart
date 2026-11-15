// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'user.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

User _$UserFromJson(Map<String, dynamic> json) => User(
  id: (json['id'] as num?)?.toInt(),
  userName: json['userName'] as String,
  email: json['email'] as String,
  phoneNumber: json['phoneNumber'] as String?,
  firstName: json['firstName'] as String,
  lastName: json['lastName'] as String,
  profileImageUrl: json['profileImageUrl'] as String?,
  isActive: json['isActive'] as bool? ?? true,
);

Map<String, dynamic> _$UserToJson(User instance) => <String, dynamic>{
  'id': instance.id,
  'userName': instance.userName,
  'email': instance.email,
  'phoneNumber': instance.phoneNumber,
  'firstName': instance.firstName,
  'lastName': instance.lastName,
  'profileImageUrl': instance.profileImageUrl,
  'isActive': instance.isActive,
};
