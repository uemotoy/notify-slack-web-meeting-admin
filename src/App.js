import React from "react";
import SideNavigation from "./SideNavigation";
import UserList from "./UserList";

const users = [
  { id: "1", name: "taro" },
  { id: "2", name: "jiro" },
  { id: "3", name: "taro" },
  { id: "4", name: "jiro" },
  { id: "5", name: "taro" },
  { id: "6", name: "taro" },
  { id: "7", name: "jiro" },
  { id: "8", name: "taro" },
  { id: "9", name: "jiro" },
  { id: "10", name: "taro" },
  { id: "11", name: "jiro" },
  { id: "12", name: "taro" },
  { id: "13", name: "jiro" },
  { id: "14", name: "taro" },
  { id: "15", name: "jiro" },
  { id: "16", name: "taro" },
  { id: "17", name: "jiro" },
  { id: "18", name: "taro" },
  { id: "19", name: "jiro" },
];

function App() {
  return (
    <div className="app">
      <SideNavigation />

      <div className="main-content">
        <UserList users={users} />
      </div>
    </div>
  );
}

export default App;
