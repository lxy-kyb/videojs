import json
A1 = {"Name":"P1", "TimeSpan":10}
A2 = {"Name":"P2", "TimeSpan":10}
A3 = {"Name":"P3", "TimeSpan":10}
A4 = {"Name":"P4", "TimeSpan":10}
A5 = {"Name":"P5", "TimeSpan":10}
A6 = {"Name":"P6", "TimeSpan":10}
A7 = {"Name":"P7", "TimeSpan":10}
A8 = {"Name":"P8", "TimeSpan":10}
A9 = {"Name":"P9", "TimeSpan":10}
A10 = {"Name":"P10", "TimeSpan":10}

List = []
List.append(A1)
List.append(A2)
List.append(A3)


print List
python_to_json = json.dumps(List,ensure_ascii=False)
print python_to_json