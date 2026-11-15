import 'package:majstorio_desktop/models/appointment/appointment.dart';
import 'package:majstorio_desktop/providers/base_provider.dart';

class AppointmentProvider extends BaseProvider<Appointment> {
  AppointmentProvider() : super("Appointments", Appointment.fromJson);
}
