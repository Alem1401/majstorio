import 'package:flutter/material.dart';
import 'package:majstorio_desktop/layouts/master_screen.dart';
import 'package:majstorio_desktop/models/appointment/appointment.dart';
import 'package:majstorio_desktop/models/base_search_request.dart';
import 'package:majstorio_desktop/providers/appointment/appointment_provider.dart';
import 'package:majstorio_desktop/utils/formatters.dart';
import 'package:provider/provider.dart';

class AppointmentsScreen extends StatefulWidget {
  const AppointmentsScreen({super.key});

  @override
  State<AppointmentsScreen> createState() => _AppointmentsScreenState();
}

class _AppointmentsScreenState extends State<AppointmentsScreen> {
  late AppointmentProvider _provider;
  var appointments = <Appointment>[];

  void loadAppointments() async {
    var response = await _provider.get(BaseSearchRequest());
    setState(() {
      appointments = response.items;
    });
  }

  @override
  void initState() {
    _provider = context.read<AppointmentProvider>();
    loadAppointments();
    super.initState();
  }

  @override
  Widget build(BuildContext context) {
    return MasterScreen(
      title: "Appointments",
      child: SingleChildScrollView(
        padding: const EdgeInsets.all(32),
        child: Card(
          child: LayoutBuilder(
            builder: (context, constraints) {
              return SingleChildScrollView(
                scrollDirection: Axis.horizontal,
                child: ConstrainedBox(
                  constraints: BoxConstraints(minWidth: constraints.maxWidth),
                  child: DataTable(
                    columns: const [
                      DataColumn(label: Text("Customer")),
                      DataColumn(label: Text("Tradesperson")),
                      DataColumn(label: Text("Category")),
                      DataColumn(label: Text("Start")),
                      DataColumn(label: Text("End")),
                      DataColumn(label: Text("Status")),
                    ],
                    rows: appointments.map((appointment) {
                      return DataRow(
                        cells: [
                          DataCell(Text(appointment.customerName)),
                          DataCell(Text(appointment.tradespersonName)),
                          DataCell(Text(appointment.serviceCategoryName)),
                          DataCell(Text(formatDateTime(appointment.startUtc))),
                          DataCell(Text(formatDateTime(appointment.endUtc))),
                          DataCell(Text(appointment.statusName)),
                        ],
                      );
                    }).toList(),
                  ),
                ),
              );
            },
          ),
        ),
      ),
    );
  }
}
